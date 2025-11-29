using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace SemaphURL.Services;

/// <summary>
/// Named hotkey identifiers
/// </summary>
public static class HotkeyNames
{
    public const string FavoriteSites = "FavoriteSites";
    public const string ClipboardUrl = "ClipboardUrl";
}

public interface IHotkeyService
{
    event Action? FavoriteSitesHotkeyPressed;
    event Action? ClipboardUrlHotkeyPressed;
    
    bool RegisterAll(string favoriteSitesHotkey, string clipboardUrlHotkey);
    void UnregisterAll();
    void ReregisterAll(string favoriteSitesHotkey, string clipboardUrlHotkey);
    
    static (uint modifiers, uint key) ParseHotkeyString(string hotkey) => HotkeyService.ParseHotkey(hotkey);
}

/// <summary>
/// Service for registering multiple global hotkeys using Win32 API
/// </summary>
public class HotkeyService : IHotkeyService, IDisposable
{
    // Win32 API imports
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    // Modifiers
    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;
    private const uint MOD_WIN = 0x0008;
    private const uint MOD_NOREPEAT = 0x4000;

    // Hotkey IDs
    private const int HOTKEY_ID_FAVORITES = 0x0001;
    private const int HOTKEY_ID_CLIPBOARD = 0x0002;

    // Windows message
    private const int WM_HOTKEY = 0x0312;

    private readonly ILoggingService _logger;
    private HwndSource? _hwndSource;
    private IntPtr _windowHandle = IntPtr.Zero;
    private readonly HashSet<int> _registeredHotkeys = [];
    private bool _disposed;

    public event Action? FavoriteSitesHotkeyPressed;
    public event Action? ClipboardUrlHotkeyPressed;

    public HotkeyService(ILoggingService logger)
    {
        _logger = logger;
    }

    public bool RegisterAll(string favoriteSitesHotkey, string clipboardUrlHotkey)
    {
        try
        {
            EnsureWindow();

            var success = true;

            // Register Favorite Sites hotkey
            if (!string.IsNullOrWhiteSpace(favoriteSitesHotkey))
            {
                var (modifiers, key) = ParseHotkey(favoriteSitesHotkey);
                if (RegisterSingleHotkey(HOTKEY_ID_FAVORITES, modifiers | MOD_NOREPEAT, key))
                {
                    _logger.LogInfo($"Hotkey '{favoriteSitesHotkey}' registered for Favorite Sites");
                }
                else
                {
                    _logger.LogError($"Failed to register hotkey '{favoriteSitesHotkey}' for Favorite Sites");
                    success = false;
                }
            }

            // Register Clipboard URL hotkey
            if (!string.IsNullOrWhiteSpace(clipboardUrlHotkey))
            {
                var (modifiers, key) = ParseHotkey(clipboardUrlHotkey);
                if (RegisterSingleHotkey(HOTKEY_ID_CLIPBOARD, modifiers | MOD_NOREPEAT, key))
                {
                    _logger.LogInfo($"Hotkey '{clipboardUrlHotkey}' registered for Clipboard URL");
                }
                else
                {
                    _logger.LogError($"Failed to register hotkey '{clipboardUrlHotkey}' for Clipboard URL");
                    success = false;
                }
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to register global hotkeys", ex);
            return false;
        }
    }

    public void UnregisterAll()
    {
        if (_windowHandle == IntPtr.Zero)
            return;

        try
        {
            foreach (var id in _registeredHotkeys.ToList())
            {
                UnregisterHotKey(_windowHandle, id);
            }
            _registeredHotkeys.Clear();
            _logger.LogInfo("All global hotkeys unregistered");
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to unregister hotkeys", ex);
        }
    }

    public void ReregisterAll(string favoriteSitesHotkey, string clipboardUrlHotkey)
    {
        UnregisterAll();
        RegisterAll(favoriteSitesHotkey, clipboardUrlHotkey);
    }

    private void EnsureWindow()
    {
        if (_hwndSource != null)
            return;

        var parameters = new HwndSourceParameters("SemaphURL_HotkeyWindow")
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
    }

    private bool RegisterSingleHotkey(int id, uint modifiers, uint key)
    {
        if (_windowHandle == IntPtr.Zero)
            return false;

        // Unregister if already registered
        if (_registeredHotkeys.Contains(id))
        {
            UnregisterHotKey(_windowHandle, id);
            _registeredHotkeys.Remove(id);
        }

        if (RegisterHotKey(_windowHandle, id, modifiers, key))
        {
            _registeredHotkeys.Add(id);
            return true;
        }

        var error = Marshal.GetLastWin32Error();
        _logger.LogError($"RegisterHotKey failed for ID {id}. Error code: {error}");
        return false;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY)
        {
            var id = wParam.ToInt32();
            handled = true;

            switch (id)
            {
                case HOTKEY_ID_FAVORITES:
                    OnFavoriteSitesHotkeyPressed();
                    break;
                case HOTKEY_ID_CLIPBOARD:
                    OnClipboardUrlHotkeyPressed();
                    break;
            }
        }
        return IntPtr.Zero;
    }

    private void OnFavoriteSitesHotkeyPressed()
    {
        try
        {
            FavoriteSitesHotkeyPressed?.Invoke();
        }
        catch (Exception ex)
        {
            _logger.LogError("Error handling Favorite Sites hotkey press", ex);
        }
    }

    private void OnClipboardUrlHotkeyPressed()
    {
        try
        {
            ClipboardUrlHotkeyPressed?.Invoke();
        }
        catch (Exception ex)
        {
            _logger.LogError("Error handling Clipboard URL hotkey press", ex);
        }
    }

    /// <summary>
    /// Parse a hotkey string like "Ctrl+Shift+Space" into modifiers and virtual key code
    /// </summary>
    public static (uint modifiers, uint key) ParseHotkey(string hotkey)
    {
        if (string.IsNullOrWhiteSpace(hotkey))
            return (0, 0);

        uint modifiers = 0;
        uint key = 0;

        var parts = hotkey.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var part in parts)
        {
            var lower = part.ToLowerInvariant();
            switch (lower)
            {
                case "ctrl":
                case "control":
                    modifiers |= MOD_CONTROL;
                    break;
                case "alt":
                    modifiers |= MOD_ALT;
                    break;
                case "shift":
                    modifiers |= MOD_SHIFT;
                    break;
                case "win":
                case "windows":
                    modifiers |= MOD_WIN;
                    break;
                default:
                    // Try to parse as a key
                    key = GetVirtualKeyCode(part);
                    break;
            }
        }

        return (modifiers, key);
    }

    /// <summary>
    /// Convert a key name to virtual key code
    /// </summary>
    private static uint GetVirtualKeyCode(string keyName)
    {
        var normalized = keyName.ToUpperInvariant().Trim();

        // Common keys
        return normalized switch
        {
            "SPACE" => 0x20,
            "ENTER" => 0x0D,
            "TAB" => 0x09,
            "ESCAPE" or "ESC" => 0x1B,
            "BACKSPACE" => 0x08,
            "DELETE" or "DEL" => 0x2E,
            "INSERT" or "INS" => 0x2D,
            "HOME" => 0x24,
            "END" => 0x23,
            "PAGEUP" or "PGUP" => 0x21,
            "PAGEDOWN" or "PGDN" => 0x22,
            "UP" => 0x26,
            "DOWN" => 0x28,
            "LEFT" => 0x25,
            "RIGHT" => 0x27,
            "F1" => 0x70,
            "F2" => 0x71,
            "F3" => 0x72,
            "F4" => 0x73,
            "F5" => 0x74,
            "F6" => 0x75,
            "F7" => 0x76,
            "F8" => 0x77,
            "F9" => 0x78,
            "F10" => 0x79,
            "F11" => 0x7A,
            "F12" => 0x7B,
            // Letters A-Z (0x41-0x5A)
            _ when normalized.Length == 1 && char.IsLetter(normalized[0]) => (uint)normalized[0],
            // Numbers 0-9 (0x30-0x39)
            _ when normalized.Length == 1 && char.IsDigit(normalized[0]) => (uint)normalized[0],
            // Try to use KeyInterop as fallback
            _ => TryParseKey(normalized)
        };
    }

    private static uint TryParseKey(string keyName)
    {
        try
        {
            if (Enum.TryParse<Key>(keyName, true, out var key))
            {
                return (uint)KeyInterop.VirtualKeyFromKey(key);
            }
        }
        catch { }
        
        return 0;
    }

    /// <summary>
    /// Format modifiers and key back to a string like "Ctrl+Shift+Space"
    /// </summary>
    public static string FormatHotkey(uint modifiers, uint key)
    {
        var parts = new List<string>();

        if ((modifiers & MOD_CONTROL) != 0) parts.Add("Ctrl");
        if ((modifiers & MOD_ALT) != 0) parts.Add("Alt");
        if ((modifiers & MOD_SHIFT) != 0) parts.Add("Shift");
        if ((modifiers & MOD_WIN) != 0) parts.Add("Win");

        var keyName = GetKeyName(key);
        if (!string.IsNullOrEmpty(keyName))
            parts.Add(keyName);

        return string.Join("+", parts);
    }

    private static string GetKeyName(uint vk)
    {
        return vk switch
        {
            0x20 => "Space",
            0x0D => "Enter",
            0x09 => "Tab",
            0x1B => "Escape",
            0x08 => "Backspace",
            0x2E => "Delete",
            0x2D => "Insert",
            0x24 => "Home",
            0x23 => "End",
            0x21 => "PageUp",
            0x22 => "PageDown",
            0x26 => "Up",
            0x28 => "Down",
            0x25 => "Left",
            0x27 => "Right",
            >= 0x70 and <= 0x7B => $"F{vk - 0x70 + 1}",
            >= 0x41 and <= 0x5A => ((char)vk).ToString(),
            >= 0x30 and <= 0x39 => ((char)vk).ToString(),
            _ => ""
        };
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
            UnregisterAll();
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
