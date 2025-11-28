using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using Microsoft.Win32;

namespace SemaphURL.Services;

public interface IBrowserRegistrationService
{
    bool IsRegistered { get; }
    bool IsRunningAsAdmin { get; }
    bool Register();
    bool Unregister();
    void OpenDefaultAppsSettings();
    void RestartAsAdmin();
}

/// <summary>
/// Service for registering SemaphURL as a browser in Windows
/// </summary>
public class BrowserRegistrationService : IBrowserRegistrationService
{
    private readonly ILoggingService _logger;
    private readonly string _exePath;

    public BrowserRegistrationService(ILoggingService logger)
    {
        _logger = logger;
        _exePath = Process.GetCurrentProcess().MainModule?.FileName 
            ?? Path.Combine(AppContext.BaseDirectory, "SemaphURL.exe");
    }

    public bool IsRunningAsAdmin
    {
        get
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }

    public bool IsRegistered
    {
        get
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Clients\StartMenuInternet\SemaphURL");
                return key != null;
            }
            catch
            {
                return false;
            }
        }
    }

    public bool Register()
    {
        try
        {
            _logger.LogInfo($"Registering SemaphURL as browser: {_exePath}");

            // User Classes - URL Protocol Handler
            using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\SemaphURL"))
            {
                key?.SetValue("", "SemaphURL URL Handler");
                key?.SetValue("URL Protocol", "");
            }

            using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\SemaphURL\DefaultIcon"))
            {
                key?.SetValue("", $"\"{_exePath}\",0");
            }

            using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\SemaphURL\shell\open\command"))
            {
                key?.SetValue("", $"\"{_exePath}\" \"%1\"");
            }

            // StartMenuInternet Registration
            using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Clients\StartMenuInternet\SemaphURL"))
            {
                key?.SetValue("", "SemaphURL");
            }

            using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Clients\StartMenuInternet\SemaphURL\DefaultIcon"))
            {
                key?.SetValue("", $"\"{_exePath}\",0");
            }

            // Capabilities
            using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Clients\StartMenuInternet\SemaphURL\Capabilities"))
            {
                key?.SetValue("ApplicationName", "SemaphURL");
                key?.SetValue("ApplicationDescription", "Smart URL Router - Routes URLs to different browsers");
                key?.SetValue("ApplicationIcon", $"\"{_exePath}\",0");
            }

            // URL Associations
            using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Clients\StartMenuInternet\SemaphURL\Capabilities\URLAssociations"))
            {
                key?.SetValue("http", "SemaphURL");
                key?.SetValue("https", "SemaphURL");
            }

            // File Associations
            using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Clients\StartMenuInternet\SemaphURL\Capabilities\FileAssociations"))
            {
                key?.SetValue(".htm", "SemaphURL");
                key?.SetValue(".html", "SemaphURL");
            }

            // Shell open command
            using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Clients\StartMenuInternet\SemaphURL\shell\open\command"))
            {
                key?.SetValue("", $"\"{_exePath}\"");
            }

            // Register in HKLM (requires admin)
            if (IsRunningAsAdmin)
            {
                try
                {
                    using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\RegisteredApplications", true);
                    key?.SetValue("SemaphURL", @"Software\Clients\StartMenuInternet\SemaphURL\Capabilities");
                }
                catch (Exception ex)
                {
                    _logger.LogError("Failed to register in HKLM (admin required)", ex);
                }
            }
            else
            {
                // Try to register in HKCU as fallback
                try
                {
                    using var key = Registry.CurrentUser.CreateSubKey(@"Software\RegisteredApplications");
                    key?.SetValue("SemaphURL", @"Software\Clients\StartMenuInternet\SemaphURL\Capabilities");
                }
                catch (Exception ex)
                {
                    _logger.LogError("Failed to register in HKCU RegisteredApplications", ex);
                }
            }

            _logger.LogInfo("Browser registration completed successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to register as browser", ex);
            return false;
        }
    }

    public bool Unregister()
    {
        try
        {
            _logger.LogInfo("Unregistering SemaphURL as browser");

            // Remove URL Protocol Handler
            try { Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\SemaphURL", false); } catch { }

            // Remove StartMenuInternet Registration
            try { Registry.CurrentUser.DeleteSubKeyTree(@"Software\Clients\StartMenuInternet\SemaphURL", false); } catch { }

            // Remove from RegisteredApplications
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\RegisteredApplications", true);
                key?.DeleteValue("SemaphURL", false);
            }
            catch { }

            if (IsRunningAsAdmin)
            {
                try
                {
                    using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\RegisteredApplications", true);
                    key?.DeleteValue("SemaphURL", false);
                }
                catch { }
            }

            _logger.LogInfo("Browser unregistration completed");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to unregister browser", ex);
            return false;
        }
    }

    public void OpenDefaultAppsSettings()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "ms-settings:defaultapps",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to open Default Apps settings", ex);
        }
    }

    public void RestartAsAdmin()
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = _exePath,
                UseShellExecute = true,
                Verb = "runas",
                Arguments = "--register"
            };
            Process.Start(startInfo);
            
            // Exit current instance
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to restart as admin", ex);
        }
    }
}
