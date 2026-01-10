using System.Windows;

namespace SemaphURL.Services;

public interface IClipboardService
{
    /// <summary>
    /// Gets the URL from clipboard if valid, otherwise returns null
    /// </summary>
    string? GetClipboardUrl();

    /// <summary>
    /// Checks if the clipboard contains a valid URL
    /// </summary>
    bool HasUrl();
}

/// <summary>
/// Service for reading and validating URLs from clipboard
/// </summary>
public class ClipboardService : IClipboardService
{
    private readonly ILoggingService _logger;

    public ClipboardService(ILoggingService logger)
    {
        _logger = logger;
    }

    public string? GetClipboardUrl()
    {
        try
        {
            if (!Clipboard.ContainsText())
            {
                return null;
            }

            var text = Clipboard.GetText()?.Trim();

            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            // Validate URL
            if (IsValidUrl(text))
            {
                _logger.LogInfo($"Valid URL found in clipboard: {text}");
                return text;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to read from clipboard", ex);
            return null;
        }
    }

    public bool HasUrl()
    {
        return GetClipboardUrl() != null;
    }

    private static bool IsValidUrl(string text)
    {
        // Quick check for common URL prefixes
        if (!text.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !text.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
            !text.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase))
        {
            // Check if it looks like a domain without protocol
            if (LooksLikeDomain(text))
            {
                return false; // We want full URLs with protocol
            }
            return false;
        }

        // Validate with Uri class
        return Uri.TryCreate(text, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp ||
                uri.Scheme == Uri.UriSchemeHttps ||
                uri.Scheme == Uri.UriSchemeFtp);
    }

    private static bool LooksLikeDomain(string text)
    {
        // Basic check for domain-like strings (e.g., "google.com")
        return text.Contains('.') &&
               !text.Contains(' ') &&
               !text.Contains('\n');
    }
}
