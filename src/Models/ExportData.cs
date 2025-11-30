namespace SemaphURL.Models;

/// <summary>
/// Data structure for exporting/importing settings
/// </summary>
public class ExportData
{
    public string Version { get; set; } = "1.0";
    public DateTime ExportDate { get; set; } = DateTime.UtcNow;
    public List<RoutingRule> Rules { get; set; } = [];
    public List<FavoriteSite> FavoriteSites { get; set; } = [];
    public ExportSettings? Settings { get; set; }
}

/// <summary>
/// Exported settings (subset of AppConfig)
/// </summary>
public class ExportSettings
{
    public string? DefaultBrowserPath { get; set; }
    public string? DefaultBrowserArguments { get; set; }
    public bool DeveloperMode { get; set; }
    public string? FavoriteSitesHotkey { get; set; }
    public string? ClipboardUrlHotkey { get; set; }
}

/// <summary>
/// Result of import operation
/// </summary>
public class ImportResult
{
    public bool Success { get; set; }
    public int RulesImported { get; set; }
    public int FavoritesImported { get; set; }
    public List<string> DisabledRules { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
    public string? ErrorMessage { get; set; }

    public bool HasWarnings => DisabledRules.Count > 0 || Warnings.Count > 0;

    public string GetSummary()
    {
        var lines = new List<string>();

        if (Success)
        {
            lines.Add($"✓ Imported {RulesImported} routing rule(s)");
            lines.Add($"✓ Imported {FavoritesImported} favorite site(s)");

            if (DisabledRules.Count > 0)
            {
                lines.Add("");
                lines.Add($"⚠ {DisabledRules.Count} rule(s) were disabled (browser not found):");
                foreach (var rule in DisabledRules)
                {
                    lines.Add($"  • {rule}");
                }
            }

            if (Warnings.Count > 0)
            {
                lines.Add("");
                lines.Add("⚠ Warnings:");
                foreach (var warning in Warnings)
                {
                    lines.Add($"  • {warning}");
                }
            }
        }
        else
        {
            lines.Add($"✗ Import failed: {ErrorMessage}");
        }

        return string.Join(Environment.NewLine, lines);
    }
}

