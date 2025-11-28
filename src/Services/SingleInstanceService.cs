using System.IO;
using System.IO.Pipes;

namespace SemaphURL.Services;

public interface ISingleInstanceService : IDisposable
{
    bool IsFirstInstance { get; }
    event Action<string>? UrlReceived;
    Task StartListeningAsync(CancellationToken cancellationToken = default);
    Task<bool> SendUrlToRunningInstanceAsync(string url);
}

/// <summary>
/// Manages single instance application with Named Pipes IPC
/// </summary>
public class SingleInstanceService : ISingleInstanceService
{
    private const string MutexName = "SemaphURL_SingleInstance_Mutex";
    private const string PipeName = "SemaphURL_IPC_Pipe";
    
    private readonly Mutex _mutex;
    private readonly bool _isFirstInstance;
    private CancellationTokenSource? _cts;
    
    public bool IsFirstInstance => _isFirstInstance;
    public event Action<string>? UrlReceived;

    public SingleInstanceService()
    {
        _mutex = new Mutex(true, MutexName, out _isFirstInstance);
    }

    public async Task StartListeningAsync(CancellationToken cancellationToken = default)
    {
        if (!_isFirstInstance)
            return;

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        
        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                await using var server = new NamedPipeServerStream(
                    PipeName,
                    PipeDirection.In,
                    NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                await server.WaitForConnectionAsync(_cts.Token);
                
                using var reader = new StreamReader(server);
                var url = await reader.ReadToEndAsync(_cts.Token);
                
                if (!string.IsNullOrWhiteSpace(url))
                {
                    UrlReceived?.Invoke(url.Trim());
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // Continue listening even if one connection fails
                await Task.Delay(100, _cts.Token);
            }
        }
    }

    public async Task<bool> SendUrlToRunningInstanceAsync(string url)
    {
        try
        {
            await using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            await client.ConnectAsync(3000); // 3 second timeout
            
            await using var writer = new StreamWriter(client);
            await writer.WriteAsync(url);
            await writer.FlushAsync();
            
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        
        if (_isFirstInstance)
        {
            _mutex.ReleaseMutex();
        }
        _mutex.Dispose();
    }
}

