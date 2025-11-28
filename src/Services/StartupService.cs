using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace SemaphURL.Services;

public interface IStartupService
{
    bool IsEnabled { get; }
    bool Enable();
    bool Disable();
}

/// <summary>
/// Service for managing Windows startup registration
/// </summary>
public class StartupService : IStartupService
{
    private const string StartupKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "SemaphURL";
    
    private readonly ILoggingService _logger;
    private readonly string _exePath;

    public StartupService(ILoggingService logger)
    {
        _logger = logger;
        _exePath = Process.GetCurrentProcess().MainModule?.FileName 
            ?? Path.Combine(AppContext.BaseDirectory, "SemaphURL.exe");
    }

    public bool IsEnabled
    {
        get
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(StartupKeyPath);
                var value = key?.GetValue(AppName)?.ToString();
                return !string.IsNullOrEmpty(value) && value.Contains("SemaphURL", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to check startup status", ex);
                return false;
            }
        }
    }

    public bool Enable()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(StartupKeyPath, writable: true);
            if (key == null)
            {
                _logger.LogError("Failed to open startup registry key");
                return false;
            }

            // Add --minimized argument to start in tray
            key.SetValue(AppName, $"\"{_exePath}\" --minimized");
            _logger.LogInfo("Enabled startup with Windows");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to enable startup", ex);
            return false;
        }
    }

    public bool Disable()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(StartupKeyPath, writable: true);
            if (key == null)
            {
                return true; // Key doesn't exist, nothing to disable
            }

            key.DeleteValue(AppName, throwOnMissingValue: false);
            _logger.LogInfo("Disabled startup with Windows");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to disable startup", ex);
            return false;
        }
    }
}

