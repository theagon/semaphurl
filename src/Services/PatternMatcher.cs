using System.Text.RegularExpressions;
using SemaphURL.Models;

namespace SemaphURL.Services;

/// <summary>
/// Static class for URL pattern matching logic.
/// Extracted for testability.
/// </summary>
public static class PatternMatcher
{
    /// <summary>
    /// Checks if a URL matches the given pattern based on pattern type.
    /// </summary>
    /// <param name="uri">Parsed URI (can be null for invalid URLs)</param>
    /// <param name="url">Original URL string</param>
    /// <param name="patternType">Type of pattern matching to use</param>
    /// <param name="pattern">Pattern to match against</param>
    /// <returns>True if the URL matches the pattern</returns>
    public static bool Matches(Uri? uri, string url, PatternType patternType, string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            return false;

        try
        {
            return patternType switch
            {
                // Basic patterns
                PatternType.DomainContains => MatchDomainContains(uri, pattern),
                PatternType.DomainEquals => MatchDomainEquals(uri, pattern),
                PatternType.UrlContains => MatchUrlContains(url, pattern),
                
                // Advanced patterns
                PatternType.DomainStartsWith => MatchDomainStartsWith(uri, pattern),
                PatternType.DomainEndsWith => MatchDomainEndsWith(uri, pattern),
                PatternType.Regex => MatchRegex(url, pattern),
                
                // Developer patterns (port-based)
                PatternType.HostPort => MatchHostPort(uri, pattern),
                PatternType.PortEquals => MatchPort(uri, pattern),
                PatternType.PortRange => MatchPortRange(uri, pattern),
                
                _ => false
            };
        }
        catch
        {
            // Invalid regex or other error
            return false;
        }
    }

    #region Basic Pattern Matchers

    private static bool MatchDomainContains(Uri? uri, string pattern)
    {
        return uri?.Host.Contains(pattern, StringComparison.OrdinalIgnoreCase) == true;
    }

    private static bool MatchDomainEquals(Uri? uri, string pattern)
    {
        return uri?.Host.Equals(pattern, StringComparison.OrdinalIgnoreCase) == true;
    }

    private static bool MatchUrlContains(string url, string pattern)
    {
        return url.Contains(pattern, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Advanced Pattern Matchers

    private static bool MatchDomainStartsWith(Uri? uri, string pattern)
    {
        return uri?.Host.StartsWith(pattern, StringComparison.OrdinalIgnoreCase) == true;
    }

    private static bool MatchDomainEndsWith(Uri? uri, string pattern)
    {
        return uri?.Host.EndsWith(pattern, StringComparison.OrdinalIgnoreCase) == true;
    }

    private static bool MatchRegex(string url, string pattern)
    {
        return Regex.IsMatch(url, pattern, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
    }

    #endregion

    #region Developer Pattern Matchers (Port-based)

    private static bool MatchHostPort(Uri? uri, string pattern)
    {
        if (uri == null) return false;
        var hostPort = $"{uri.Host}:{uri.Port}";
        return hostPort.Equals(pattern, StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchPort(Uri? uri, string pattern)
    {
        if (uri == null) return false;
        return uri.Port.ToString() == pattern;
    }

    private static bool MatchPortRange(Uri? uri, string pattern)
    {
        if (uri == null) return false;
        
        var parts = pattern.Split('-');
        if (parts.Length != 2) return false;
        
        if (!int.TryParse(parts[0].Trim(), out var minPort) || 
            !int.TryParse(parts[1].Trim(), out var maxPort))
            return false;
            
        return uri.Port >= minPort && uri.Port <= maxPort;
    }

    #endregion
}

