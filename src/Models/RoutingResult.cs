namespace SemaphURL.Models;

/// <summary>
/// Result of URL routing operation
/// </summary>
public class RoutingResult
{
    public required string OriginalUrl { get; init; }
    public required string BrowserPath { get; init; }
    public required string Arguments { get; init; }
    public RoutingRule? MatchedRule { get; init; }
    public bool IsDefaultBrowser { get; init; }
    public bool IsSystemFallback { get; init; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    public string GetRuleDescription()
    {
        if (IsSystemFallback)
            return "System default browser (fallback)";
        if (IsDefaultBrowser)
            return "Default browser";
        return MatchedRule?.Name ?? "Unknown rule";
    }
}

