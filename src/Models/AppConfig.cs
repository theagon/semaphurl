namespace SemaphURL.Models;

/// <summary>
/// Application configuration stored in JSON
/// </summary>
public class AppConfig
{
    public string DefaultBrowserPath { get; set; } = string.Empty;
    public string DefaultBrowserArguments { get; set; } = "\"{url}\"";
    public List<RoutingRule> Rules { get; set; } = [];
    public bool MinimizeToTrayOnClose { get; set; } = true;
    public bool StartMinimized { get; set; } = false;
    public bool ShowNotifications { get; set; } = true;
    public bool StartWithWindows { get; set; } = false;
    public List<FavoriteSite> FavoriteSites { get; set; } = [];
    public string FavoriteSitesHotkey { get; set; } = "Ctrl+Space";
    public string ClipboardUrlHotkey { get; set; } = "Ctrl+Shift+Space";
    public bool FocusBrowserAfterRouting { get; set; } = true;
    public bool DeveloperMode { get; set; } = false;
}

