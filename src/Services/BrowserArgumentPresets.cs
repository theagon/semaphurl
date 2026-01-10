using System.IO;

namespace SemaphURL.Services;

/// <summary>
/// Predefined browser argument presets for common scenarios
/// </summary>
public static class BrowserArgumentPresets
{
    public static readonly string[] PresetNames = ["Default", "Incognito/Private", "New Window"];

    /// <summary>
    /// Gets the argument template for a given preset and browser executable
    /// </summary>
    public static string GetArguments(string presetName, string browserPath)
    {
        var browserType = DetectBrowserType(browserPath);

        return presetName switch
        {
            "Default" => "\"{url}\"",
            "Incognito/Private" => GetIncognitoArgs(browserType),
            "New Window" => GetNewWindowArgs(browserType),
            _ => "\"{url}\""
        };
    }

    private static string GetIncognitoArgs(BrowserType browserType)
    {
        return browserType switch
        {
            BrowserType.Chrome => "--incognito \"{url}\"",
            BrowserType.Edge => "--inprivate \"{url}\"",
            BrowserType.Firefox => "-private-window \"{url}\"",
            BrowserType.Brave => "--incognito \"{url}\"",
            BrowserType.Opera => "--private \"{url}\"",
            BrowserType.Vivaldi => "--incognito \"{url}\"",
            _ => "\"{url}\""
        };
    }

    private static string GetNewWindowArgs(BrowserType browserType)
    {
        return browserType switch
        {
            BrowserType.Chrome => "--new-window \"{url}\"",
            BrowserType.Edge => "--new-window \"{url}\"",
            BrowserType.Firefox => "-new-window \"{url}\"",
            BrowserType.Brave => "--new-window \"{url}\"",
            BrowserType.Opera => "--new-window \"{url}\"",
            BrowserType.Vivaldi => "--new-window \"{url}\"",
            _ => "\"{url}\""
        };
    }

    private static BrowserType DetectBrowserType(string browserPath)
    {
        if (string.IsNullOrEmpty(browserPath))
            return BrowserType.Unknown;

        var fileName = Path.GetFileNameWithoutExtension(browserPath).ToLowerInvariant();

        return fileName switch
        {
            "chrome" => BrowserType.Chrome,
            "msedge" => BrowserType.Edge,
            "firefox" => BrowserType.Firefox,
            "brave" => BrowserType.Brave,
            "opera" => BrowserType.Opera,
            "vivaldi" => BrowserType.Vivaldi,
            _ => BrowserType.Unknown
        };
    }

    private enum BrowserType
    {
        Unknown,
        Chrome,
        Edge,
        Firefox,
        Brave,
        Opera,
        Vivaldi
    }
}
