using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace SemaphURL.Services;

public interface IHotkeyService
{
    event Action? HotkeyPressed;
    bool Register();
    void Unregister();
}

/// <summary>
/// Service for registering global hotkeys using Win32 API
/// </summary>
public class HotkeyService : IHotkeyService, IDisposable
{
    // Win32 API imports
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    // Modifiers
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_NOREPEAT = 0x4000;

    // Virtual key codes
    private const uint VK_SPACE = 0x20;

    // Hotkey ID
    private const int HOTKEY_ID = 0x0001;

    // Windows message
    private const int WM_HOTKEY = 0x0312;

    private readonly ILoggingService _logger;
    private HwndSource? _hwndSource;
    private IntPtr _windowHandle = IntPtr.Zero;
    private bool _isRegistered;
    private bool _disposed;

    public event Action? HotkeyPressed;

    public HotkeyService(ILoggingService logger)
    {
        _logger = logger;
    }

    public bool Register()
    {
        if (_isRegistered)
            return true;

        try
        {
            // Create a hidden window to receive hotkey messages
            var parameters = new HwndSourceParameters("SemaphURL_HotkeyWindow")
            {
                Width = 0,
                Height = 0,
                PositionX = -100,
                PositionY = -100,
                WindowStyle = 0, // WS_OVERLAPPED
                ExtendedWindowStyle = 0
            };

            _hwndSource = new HwndSource(parameters);
            _hwndSource.AddHook(WndProc);
            _windowHandle = _hwndSource.Handle;

            // Register Ctrl+Space hotkey
            var modifiers = MOD_CONTROL | MOD_NOREPEAT;
            if (RegisterHotKey(_windowHandle, HOTKEY_ID, modifiers, VK_SPACE))
            {
                _isRegistered = true;
                _logger.LogInfo("Global hotkey Ctrl+Space registered successfully");
                return true;
            }
            else
            {
                var error = Marshal.GetLastWin32Error();
                _logger.LogError($"Failed to register hotkey. Error code: {error}");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to register global hotkey", ex);
            return false;
        }
    }

    public void Unregister()
    {
        if (!_isRegistered || _windowHandle == IntPtr.Zero)
            return;

        try
        {
            UnregisterHotKey(_windowHandle, HOTKEY_ID);
            _isRegistered = false;
            _logger.LogInfo("Global hotkey unregistered");
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to unregister hotkey", ex);
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
        {
            handled = true;
            OnHotkeyPressed();
        }
        return IntPtr.Zero;
    }

    private void OnHotkeyPressed()
    {
        try
        {
            HotkeyPressed?.Invoke();
        }
        catch (Exception ex)
        {
            _logger.LogError("Error handling hotkey press", ex);
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            Unregister();
            _hwndSource?.RemoveHook(WndProc);
            _hwndSource?.Dispose();
            _hwndSource = null;
        }

        _disposed = true;
    }

    ~HotkeyService()
    {
        Dispose(false);
    }
}

