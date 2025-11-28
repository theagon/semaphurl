using System.IO;
using System.Text;

namespace SemaphURL.Services;

public interface ILoggingService
{
    void Log(string message);
    void LogRouting(string url, string? ruleName, string browserPath);
    void LogError(string message, Exception? ex = null);
    void LogInfo(string message);
}

/// <summary>
/// Logging service with UTF-8 encoding and 5MB size limit
/// </summary>
public class LoggingService : ILoggingService
{
    private const long MaxLogSizeBytes = 5 * 1024 * 1024; // 5 MB
    private readonly string _logPath;
    private readonly object _lock = new();

    public LoggingService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var logDir = Path.Combine(appDataPath, "SemaphURL");
        Directory.CreateDirectory(logDir);
        _logPath = Path.Combine(logDir, "log.txt");
    }

    public void Log(string message)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var line = $"[{timestamp}] {message}";
        WriteToLog(line);
    }

    public void LogRouting(string url, string? ruleName, string browserPath)
    {
        var ruleInfo = ruleName ?? "Default";
        Log($"URL: {url} | Rule: {ruleInfo} | Browser: {browserPath}");
    }

    public void LogError(string message, Exception? ex = null)
    {
        var errorMsg = ex != null ? $"{message}: {ex.Message}" : message;
        Log($"ERROR: {errorMsg}");
    }

    public void LogInfo(string message)
    {
        Log($"INFO: {message}");
    }

    private void WriteToLog(string line)
    {
        lock (_lock)
        {
            try
            {
                CheckAndTruncateLog();
                File.AppendAllText(_logPath, line + Environment.NewLine, Encoding.UTF8);
            }
            catch
            {
                // Silently fail if logging fails
            }
        }
    }

    private void CheckAndTruncateLog()
    {
        try
        {
            if (!File.Exists(_logPath))
                return;

            var fileInfo = new FileInfo(_logPath);
            if (fileInfo.Length > MaxLogSizeBytes)
            {
                // Keep last 1MB of log
                var content = File.ReadAllText(_logPath, Encoding.UTF8);
                var keepBytes = 1024 * 1024;
                if (content.Length > keepBytes)
                {
                    var truncated = content[^keepBytes..];
                    var firstNewLine = truncated.IndexOf('\n');
                    if (firstNewLine > 0)
                    {
                        truncated = truncated[(firstNewLine + 1)..];
                    }
                    File.WriteAllText(_logPath, $"[Log truncated at {DateTime.Now:yyyy-MM-dd HH:mm:ss}]\n{truncated}", Encoding.UTF8);
                }
            }
        }
        catch
        {
            // If truncation fails, try to clear the log
            try { File.WriteAllText(_logPath, "", Encoding.UTF8); } catch { }
        }
    }
}

