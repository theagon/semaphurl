using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using SemaphURL.Models;

namespace SemaphURL.Services;

public interface IConfigurationService
{
    AppConfig Config { get; }
    Task LoadAsync();
    Task SaveAsync();
    string ConfigPath { get; }
}

/// <summary>
/// Service for loading and saving application configuration
/// </summary>
public class ConfigurationService : IConfigurationService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public string ConfigPath { get; }
    public AppConfig Config { get; private set; } = new();

    public ConfigurationService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var configDir = Path.Combine(appDataPath, "SemaphURL");
        Directory.CreateDirectory(configDir);
        ConfigPath = Path.Combine(configDir, "config.json");
    }

    public async Task LoadAsync()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var json = await File.ReadAllTextAsync(ConfigPath);
                Config = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? new AppConfig();
            }
            else
            {
                Config = CreateDefaultConfig();
                await SaveAsync();
            }
        }
        catch
        {
            Config = new AppConfig();
        }
    }

    public async Task SaveAsync()
    {
        try
        {
            var json = JsonSerializer.Serialize(Config, JsonOptions);
            await File.WriteAllTextAsync(ConfigPath, json);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to save configuration: {ex.Message}", ex);
        }
    }

    private static AppConfig CreateDefaultConfig() => new()
    {
        DefaultBrowserPath = FindDefaultBrowser(),
        DefaultBrowserArguments = "\"{url}\"",
        Rules = [],
        MinimizeToTrayOnClose = true,
        StartMinimized = false,
        ShowNotifications = true,
        StartWithWindows = false,
        FavoriteSites = CreateDefaultFavoriteSites(),
        FavoriteSitesHotkey = "Ctrl+Space"
    };

    private static List<FavoriteSite> CreateDefaultFavoriteSites() =>
    [
        new FavoriteSite("YouTube", "https://youtube.com", 0),
        new FavoriteSite("GitHub", "https://github.com", 1),
        new FavoriteSite("Reddit", "https://reddit.com", 2),
        new FavoriteSite("X (Twitter)", "https://x.com", 3),
        new FavoriteSite("Facebook", "https://facebook.com", 4),
        new FavoriteSite("Instagram", "https://instagram.com", 5),
        new FavoriteSite("LinkedIn", "https://linkedin.com", 6),
        new FavoriteSite("Stack Overflow", "https://stackoverflow.com", 7),
    ];

    private static string FindDefaultBrowser()
    {
        // Try to find common browsers
        string[] commonPaths =
        [
            @"C:\Program Files\Google\Chrome\Application\chrome.exe",
            @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe",
            @"C:\Program Files\Mozilla Firefox\firefox.exe",
            @"C:\Program Files (x86)\Mozilla Firefox\firefox.exe",
            @"C:\Program Files\Microsoft\Edge\Application\msedge.exe",
            @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"
        ];

        foreach (var path in commonPaths)
        {
            if (File.Exists(path))
                return path;
        }

        return string.Empty;
    }
}

