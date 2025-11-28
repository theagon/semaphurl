namespace SemaphURL.Models;

/// <summary>
/// Represents a single URL history entry
/// </summary>
public class UrlHistoryEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Url { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string BrowserPath { get; set; } = string.Empty;
    public string BrowserName { get; set; } = string.Empty;
    public string? RuleName { get; set; }

    public UrlHistoryEntry() { }

    public UrlHistoryEntry(string url, string browserPath, string browserName, string? ruleName = null)
    {
        Url = url;
        Domain = ExtractDomain(url);
        BrowserPath = browserPath;
        BrowserName = browserName;
        RuleName = ruleName;
        Timestamp = DateTime.UtcNow;
    }

    private static string ExtractDomain(string url)
    {
        try
        {
            if (!url.Contains("://"))
                url = "https://" + url;
            
            var uri = new Uri(url);
            var host = uri.Host;
            
            // Remove www. prefix
            if (host.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
                host = host[4..];
            
            return host;
        }
        catch
        {
            return string.Empty;
        }
    }
}

/// <summary>
/// Container for URL history data stored in JSON
/// </summary>
public class UrlHistoryData
{
    public List<UrlHistoryEntry> Entries { get; set; } = [];
    public DateTime LastCleanup { get; set; } = DateTime.UtcNow;
}

