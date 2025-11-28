using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using SemaphURL.Models;

namespace SemaphURL.Services;

public interface IBrowserDiscoveryService
{
    IReadOnlyList<InstalledBrowser> GetInstalledBrowsers();
    void RefreshBrowserList();
}

/// <summary>
/// Service for discovering installed browsers from Windows Registry
/// </summary>
public partial class BrowserDiscoveryService : IBrowserDiscoveryService
{
    private List<InstalledBrowser>? _cachedBrowsers;
    private readonly ILoggingService _logger;

    public BrowserDiscoveryService(ILoggingService logger)
    {
        _logger = logger;
    }

    public IReadOnlyList<InstalledBrowser> GetInstalledBrowsers()
    {
        if (_cachedBrowsers == null)
        {
            RefreshBrowserList();
        }
        return _cachedBrowsers!;
    }

    public void RefreshBrowserList()
    {
        var browsers = new Dictionary<string, InstalledBrowser>(StringComparer.OrdinalIgnoreCase);

        // Search in HKLM\SOFTWARE\Clients\StartMenuInternet
        SearchStartMenuInternet(Registry.LocalMachine, @"SOFTWARE\Clients\StartMenuInternet", browsers);
        
        // Search in HKCU\SOFTWARE\Clients\StartMenuInternet
        SearchStartMenuInternet(Registry.CurrentUser, @"SOFTWARE\Clients\StartMenuInternet", browsers);

        // Search in HKLM\SOFTWARE\WOW6432Node\Clients\StartMenuInternet (for 32-bit browsers on 64-bit Windows)
        SearchStartMenuInternet(Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Clients\StartMenuInternet", browsers);

        // Also check common browser locations that might not be registered
        AddCommonBrowsersIfMissing(browsers);

        // Filter out SemaphURL itself
        var semaphUrlKeys = browsers
            .Where(kvp => kvp.Value.Name.Contains("SemaphURL", StringComparison.OrdinalIgnoreCase) ||
                          kvp.Value.RegistryKey.Contains("SemaphURL", StringComparison.OrdinalIgnoreCase) ||
                          kvp.Key.Contains("SemaphURL", StringComparison.OrdinalIgnoreCase))
            .Select(kvp => kvp.Key)
            .ToList();
        
        foreach (var key in semaphUrlKeys)
        {
            browsers.Remove(key);
        }

        _cachedBrowsers = browsers.Values
            .OrderBy(b => b.Name)
            .ToList();

        _logger.LogInfo($"Discovered {_cachedBrowsers.Count} installed browsers");
    }

    private void SearchStartMenuInternet(RegistryKey root, string path, Dictionary<string, InstalledBrowser> browsers)
    {
        try
        {
            using var key = root.OpenSubKey(path);
            if (key == null) return;

            foreach (var browserKeyName in key.GetSubKeyNames())
            {
                try
                {
                    using var browserKey = key.OpenSubKey(browserKeyName);
                    if (browserKey == null) continue;

                    var exePath = GetBrowserExePath(browserKey);
                    if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
                        continue;

                    // Skip if we already have this exe
                    if (browsers.Values.Any(b => b.ExePath.Equals(exePath, StringComparison.OrdinalIgnoreCase)))
                        continue;

                    var name = GetBrowserName(browserKey, browserKeyName);
                    var iconPath = GetBrowserIcon(browserKey);

                    browsers[exePath] = new InstalledBrowser
                    {
                        Name = name,
                        ExePath = exePath,
                        IconPath = iconPath,
                        RegistryKey = browserKeyName
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error reading browser key {browserKeyName}", ex);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error searching registry path {path}", ex);
        }
    }

    private static string? GetBrowserExePath(RegistryKey browserKey)
    {
        // Try shell\open\command first
        using var commandKey = browserKey.OpenSubKey(@"shell\open\command");
        var command = commandKey?.GetValue("")?.ToString();

        if (!string.IsNullOrEmpty(command))
        {
            return ExtractExePath(command);
        }

        return null;
    }

    private static string? ExtractExePath(string command)
    {
        // Handle quoted paths: "C:\Path\browser.exe" --args
        if (command.StartsWith('"'))
        {
            var endQuote = command.IndexOf('"', 1);
            if (endQuote > 1)
            {
                return command.Substring(1, endQuote - 1);
            }
        }

        // Handle unquoted paths: C:\Path\browser.exe --args
        var spaceIndex = command.IndexOf(' ');
        if (spaceIndex > 0)
        {
            var potentialPath = command[..spaceIndex];
            if (File.Exists(potentialPath))
                return potentialPath;
        }

        // Maybe the whole thing is a path
        if (File.Exists(command))
            return command;

        // Try to find .exe in the command
        var match = ExePathRegex().Match(command);
        if (match.Success)
        {
            var path = match.Groups[1].Value;
            if (File.Exists(path))
                return path;
        }

        return null;
    }

    private static string GetBrowserName(RegistryKey browserKey, string fallbackName)
    {
        // Try Capabilities\ApplicationName
        using var capKey = browserKey.OpenSubKey("Capabilities");
        var appName = capKey?.GetValue("ApplicationName")?.ToString();
        if (!string.IsNullOrEmpty(appName))
            return appName;

        // Try default value of the key
        var defaultValue = browserKey.GetValue("")?.ToString();
        if (!string.IsNullOrEmpty(defaultValue))
            return defaultValue;

        // Use the key name, cleaned up
        return CleanBrowserName(fallbackName);
    }

    private static string CleanBrowserName(string name)
    {
        // Remove common suffixes
        name = name.Replace(".exe", "", StringComparison.OrdinalIgnoreCase);
        name = name.Replace("HTML", "", StringComparison.OrdinalIgnoreCase);
        
        // Add spaces before capital letters
        name = Regex.Replace(name, "([a-z])([A-Z])", "$1 $2");
        
        return name.Trim();
    }

    private static string? GetBrowserIcon(RegistryKey browserKey)
    {
        using var iconKey = browserKey.OpenSubKey("DefaultIcon");
        return iconKey?.GetValue("")?.ToString();
    }

    private void AddCommonBrowsersIfMissing(Dictionary<string, InstalledBrowser> browsers)
    {
        var commonBrowsers = new[]
        {
            ("Google Chrome", @"C:\Program Files\Google\Chrome\Application\chrome.exe"),
            ("Google Chrome", @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe"),
            ("Mozilla Firefox", @"C:\Program Files\Mozilla Firefox\firefox.exe"),
            ("Mozilla Firefox", @"C:\Program Files (x86)\Mozilla Firefox\firefox.exe"),
            ("Microsoft Edge", @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"),
            ("Microsoft Edge", @"C:\Program Files\Microsoft\Edge\Application\msedge.exe"),
            ("Brave", @"C:\Program Files\BraveSoftware\Brave-Browser\Application\brave.exe"),
            ("Brave", @"C:\Program Files (x86)\BraveSoftware\Brave-Browser\Application\brave.exe"),
            ("Opera", @"C:\Program Files\Opera\opera.exe"),
            ("Opera", @"C:\Program Files (x86)\Opera\opera.exe"),
            ("Vivaldi", @"C:\Program Files\Vivaldi\Application\vivaldi.exe"),
            ("Vivaldi", @"C:\Program Files (x86)\Vivaldi\Application\vivaldi.exe"),
            ("Yandex Browser", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Yandex\YandexBrowser\Application\browser.exe")),
            ("Arc", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Arc\Application\Arc.exe")),
            ("Thorium", @"C:\Program Files\Thorium\Application\thorium.exe"),
            ("Ungoogled Chromium", @"C:\Program Files\Chromium\Application\chrome.exe"),
        };

        foreach (var (name, path) in commonBrowsers)
        {
            if (File.Exists(path) && !browsers.ContainsKey(path))
            {
                browsers[path] = new InstalledBrowser
                {
                    Name = name,
                    ExePath = path,
                    IconPath = $"\"{path}\",0"
                };
            }
        }
    }

    [GeneratedRegex(@"([A-Za-z]:\\[^""*<>|]+\.exe)", RegexOptions.IgnoreCase)]
    private static partial Regex ExePathRegex();
}

