using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace SemaphURL.Services;

public interface IClipboardService
{
    /// <summary>
    /// Gets the URL from clipboard if valid, otherwise returns null
    /// </summary>
    string? GetClipboardUrl();
    
    /// <summary>
    /// Checks if the clipboard contains a valid URL
    /// </summary>
    bool HasUrl();

    /// <summary>
    /// Event fired when a URL is detected in the clipboard
    /// </summary>
    event Action<string>? UrlDetected;

    /// <summary>
    /// Start monitoring clipboard for URLs
    /// </summary>
    void StartMonitoring();

    /// <summary>
    /// Stop monitoring clipboard
    /// </summary>
    void StopMonitoring();
}

/// <summary>
/// Service for reading, validating, and monitoring URLs from clipboard
/// </summary>
public class ClipboardService : IClipboardService, IDisposable
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool AddClipboardFormatListener(IntPtr hwnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

    private const int WM_CLIPBOARDUPDATE = 0x031D;

    private readonly ILoggingService _logger;
    private HwndSource? _hwndSource;
    private IntPtr _windowHandle = IntPtr.Zero;
    private string? _lastDetectedUrl;
    private bool _isMonitoring;

    public event Action<string>? UrlDetected;

    public ClipboardService(ILoggingService logger)
    {
        _logger = logger;
    }

    public void StartMonitoring()
    {
        if (_isMonitoring)
            return;

        try
        {
            var parameters = new HwndSourceParameters("SemaphURL_ClipboardMonitor")
            {
                Width = 0,
                Height = 0,
                PositionX = -100,
                PositionY = -100,
                WindowStyle = 0,
                ExtendedWindowStyle = 0
            };

            _hwndSource = new HwndSource(parameters);
            _hwndSource.AddHook(WndProc);
            _windowHandle = _hwndSource.Handle;

            if (AddClipboardFormatListener(_windowHandle))
            {
                _isMonitoring = true;
                _logger.LogInfo("Clipboard monitoring started");
            }
            else
            {
                _logger.LogError($"Failed to start clipboard monitoring. Error: {Marshal.GetLastWin32Error()}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to start clipboard monitoring", ex);
        }
    }

    public void StopMonitoring()
    {
        if (!_isMonitoring || _windowHandle == IntPtr.Zero)
            return;

        try
        {
            RemoveClipboardFormatListener(_windowHandle);
            _hwndSource?.RemoveHook(WndProc);
            _hwndSource?.Dispose();
            _hwndSource = null;
            _isMonitoring = false;
            _logger.LogInfo("Clipboard monitoring stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to stop clipboard monitoring", ex);
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_CLIPBOARDUPDATE)
        {
            handled = true;
            OnClipboardChanged();
        }
        return IntPtr.Zero;
    }

    private void OnClipboardChanged()
    {
        try
        {
            var url = GetClipboardUrl();
            
            // Only notify if it's a new URL (avoid duplicate notifications)
            if (!string.IsNullOrEmpty(url) && url != _lastDetectedUrl)
            {
                _lastDetectedUrl = url;
                _logger.LogInfo($"URL detected in clipboard: {url}");
                UrlDetected?.Invoke(url);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Error processing clipboard change", ex);
        }
    }

    public void Dispose()
    {
        StopMonitoring();
    }

    public string? GetClipboardUrl()
    {
        try
        {
            if (!Clipboard.ContainsText())
            {
                return null;
            }

            var text = Clipboard.GetText()?.Trim();
            
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            // Validate URL
            if (IsValidUrl(text))
            {
                _logger.LogInfo($"Valid URL found in clipboard: {text}");
                return text;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to read from clipboard", ex);
            return null;
        }
    }

    public bool HasUrl()
    {
        return GetClipboardUrl() != null;
    }

    private static bool IsValidUrl(string text)
    {
        // Quick check for common URL prefixes
        if (!text.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !text.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
            !text.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase))
        {
            // Check if it looks like a domain without protocol
            if (LooksLikeDomain(text))
            {
                return false; // We want full URLs with protocol
            }
            return false;
        }

        // Validate with Uri class
        return Uri.TryCreate(text, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || 
                uri.Scheme == Uri.UriSchemeHttps ||
                uri.Scheme == Uri.UriSchemeFtp);
    }

    private static bool LooksLikeDomain(string text)
    {
        // Basic check for domain-like strings (e.g., "google.com")
        return text.Contains('.') && 
               !text.Contains(' ') && 
               !text.Contains('\n');
    }
}

