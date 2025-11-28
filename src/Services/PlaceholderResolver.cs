namespace SemaphURL.Services;

/// <summary>
/// Resolves placeholders in browser argument templates
/// Supported placeholders:
/// - {url} - Full URL
/// - {domain} - Domain/host part
/// - {path} - Path part of URL
/// - {query} - Query string (without ?)
/// - {scheme} - Protocol (http/https)
/// - {port} - Port number (empty if default)
/// </summary>
public static class PlaceholderResolver
{
    public static string Resolve(string template, string url)
    {
        if (string.IsNullOrWhiteSpace(template))
            return $"\"{url}\"";

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            // If URL is invalid, just replace {url} placeholder
            return template.Replace("{url}", url);
        }

        var result = template;
        
        result = result.Replace("{url}", url);
        result = result.Replace("{domain}", uri.Host);
        result = result.Replace("{path}", uri.AbsolutePath);
        result = result.Replace("{query}", uri.Query.TrimStart('?'));
        result = result.Replace("{scheme}", uri.Scheme);
        result = result.Replace("{port}", uri.IsDefaultPort ? "" : uri.Port.ToString());

        return result;
    }

    public static IReadOnlyList<string> GetAvailablePlaceholders() =>
    [
        "{url} - Full URL",
        "{domain} - Domain/host",
        "{path} - Path",
        "{query} - Query string",
        "{scheme} - Protocol",
        "{port} - Port number"
    ];
}

