using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using SemaphURL.Models;

namespace SemaphURL.Services;

public interface IRoutingService
{
    RoutingResult Route(string url);
    RoutingResult TestRoute(string url);
    Task<bool> ExecuteRoutingAsync(RoutingResult result);
}

/// <summary>
/// Service for URL routing and browser launching
/// </summary>
public class RoutingService : IRoutingService
{
    private readonly IConfigurationService _config;
    private readonly ILoggingService _logger;
    private readonly IUrlHistoryService _history;

    public RoutingService(IConfigurationService config, ILoggingService logger, IUrlHistoryService history)
    {
        _config = config;
        _logger = logger;
        _history = history;
    }

    public RoutingResult Route(string url)
    {
        return FindRoute(url);
    }

    public RoutingResult TestRoute(string url)
    {
        return FindRoute(url);
    }

    private RoutingResult FindRoute(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return new RoutingResult
            {
                OriginalUrl = url,
                BrowserPath = string.Empty,
                Arguments = string.Empty,
                Success = false,
                ErrorMessage = "URL is empty"
            };
        }

        // Validate URL
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            // Try adding https:// prefix
            if (!url.Contains("://"))
            {
                url = "https://" + url;
                Uri.TryCreate(url, UriKind.Absolute, out uri);
            }
        }

        // Get enabled rules sorted by order
        var rules = _config.Config.Rules
            .Where(r => r.Enabled)
            .OrderBy(r => r.Order)
            .ToList();

        // Try to match rules
        foreach (var rule in rules)
        {
            if (MatchesRule(uri, url, rule))
            {
                var args = PlaceholderResolver.Resolve(rule.BrowserArgumentsTemplate, url);
                return new RoutingResult
                {
                    OriginalUrl = url,
                    BrowserPath = rule.BrowserPath,
                    Arguments = args,
                    MatchedRule = rule,
                    IsDefaultBrowser = false,
                    IsSystemFallback = false,
                    Success = true
                };
            }
        }

        // No rule matched - use default browser
        if (!string.IsNullOrWhiteSpace(_config.Config.DefaultBrowserPath) && 
            File.Exists(_config.Config.DefaultBrowserPath))
        {
            var args = PlaceholderResolver.Resolve(_config.Config.DefaultBrowserArguments, url);
            return new RoutingResult
            {
                OriginalUrl = url,
                BrowserPath = _config.Config.DefaultBrowserPath,
                Arguments = args,
                MatchedRule = null,
                IsDefaultBrowser = true,
                IsSystemFallback = false,
                Success = true
            };
        }

        // Fallback to system handler
        return new RoutingResult
        {
            OriginalUrl = url,
            BrowserPath = url, // Will be opened via shell
            Arguments = string.Empty,
            MatchedRule = null,
            IsDefaultBrowser = false,
            IsSystemFallback = true,
            Success = true
        };
    }

    private static bool MatchesRule(Uri? uri, string url, RoutingRule rule)
    {
        if (string.IsNullOrWhiteSpace(rule.Pattern))
            return false;

        try
        {
            return rule.PatternType switch
            {
                PatternType.DomainContains => uri?.Host.Contains(rule.Pattern, StringComparison.OrdinalIgnoreCase) == true,
                PatternType.DomainEquals => uri?.Host.Equals(rule.Pattern, StringComparison.OrdinalIgnoreCase) == true,
                PatternType.DomainStartsWith => uri?.Host.StartsWith(rule.Pattern, StringComparison.OrdinalIgnoreCase) == true,
                PatternType.DomainEndsWith => uri?.Host.EndsWith(rule.Pattern, StringComparison.OrdinalIgnoreCase) == true,
                PatternType.UrlContains => url.Contains(rule.Pattern, StringComparison.OrdinalIgnoreCase),
                PatternType.Regex => Regex.IsMatch(url, rule.Pattern, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)),
                _ => false
            };
        }
        catch
        {
            // Invalid regex or other error
            return false;
        }
    }

    public async Task<bool> ExecuteRoutingAsync(RoutingResult result)
    {
        if (!result.Success)
        {
            _logger.LogError($"Routing failed: {result.ErrorMessage}");
            return false;
        }

        try
        {
            string browserName;
            string browserPath;
            
            if (result.IsSystemFallback)
            {
                // Open via system shell (default browser)
                _logger.LogRouting(result.OriginalUrl, "System Fallback", "System Default");
                Process.Start(new ProcessStartInfo
                {
                    FileName = result.OriginalUrl,
                    UseShellExecute = true
                });
                
                browserName = "System Default";
                browserPath = "System";
            }
            else
            {
                // Check if browser exists
                if (!File.Exists(result.BrowserPath))
                {
                    _logger.LogError($"Browser not found: {result.BrowserPath}, falling back to system handler");
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = result.OriginalUrl,
                        UseShellExecute = true
                    });
                    
                    // Record history with fallback browser
                    await RecordHistoryAsync(result.OriginalUrl, "System", "System Default (Fallback)", result.MatchedRule?.Name);
                    return true;
                }

                _logger.LogRouting(result.OriginalUrl, result.MatchedRule?.Name, result.BrowserPath);
                
                Process.Start(new ProcessStartInfo
                {
                    FileName = result.BrowserPath,
                    Arguments = result.Arguments,
                    UseShellExecute = false
                });
                
                browserPath = result.BrowserPath;
                browserName = Path.GetFileNameWithoutExtension(result.BrowserPath);
            }

            // Record to history
            await RecordHistoryAsync(result.OriginalUrl, browserPath, browserName, result.MatchedRule?.Name);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to launch browser for URL: {result.OriginalUrl}", ex);
            
            // Try system fallback
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = result.OriginalUrl,
                    UseShellExecute = true
                });
                _logger.LogInfo($"Opened URL via system fallback: {result.OriginalUrl}");
                
                // Record history with fallback
                await RecordHistoryAsync(result.OriginalUrl, "System", "System Default (Fallback)", null);
                return true;
            }
            catch (Exception fallbackEx)
            {
                _logger.LogError("System fallback also failed", fallbackEx);
                return false;
            }
        }
    }

    private async Task RecordHistoryAsync(string url, string browserPath, string browserName, string? ruleName)
    {
        try
        {
            await _history.AddEntryAsync(url, browserPath, browserName, ruleName);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to record URL history", ex);
        }
    }
}

