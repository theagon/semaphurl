using System.IO;
using System.Net.Http;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SemaphURL.Services;

public interface IFaviconService
{
    Task<ImageSource?> GetFaviconAsync(string url);
    ImageSource? GetCachedFavicon(string url);
    string GetFaviconCachePath(string domain);
}

/// <summary>
/// Service for downloading and caching website favicons
/// </summary>
public class FaviconService : IFaviconService
{
    private readonly ILoggingService _logger;
    private readonly string _cacheDir;
    private readonly HttpClient _httpClient;
    private readonly Dictionary<string, ImageSource?> _memoryCache = new(StringComparer.OrdinalIgnoreCase);

    // Popular site favicons bundled (base64 or fallback colors)
    private static readonly Dictionary<string, string> PopularSiteFallbacks = new(StringComparer.OrdinalIgnoreCase)
    {
        { "youtube.com", "#FF0000" },
        { "github.com", "#24292E" },
        { "reddit.com", "#FF4500" },
        { "twitter.com", "#1DA1F2" },
        { "x.com", "#000000" },
        { "facebook.com", "#1877F2" },
        { "instagram.com", "#E4405F" },
        { "linkedin.com", "#0A66C2" },
        { "stackoverflow.com", "#F48024" },
        { "google.com", "#4285F4" },
        { "amazon.com", "#FF9900" },
        { "netflix.com", "#E50914" },
        { "twitch.tv", "#9146FF" },
        { "discord.com", "#5865F2" },
        { "spotify.com", "#1DB954" },
        { "wikipedia.org", "#000000" },
        { "medium.com", "#000000" },
        { "notion.so", "#000000" },
        { "figma.com", "#F24E1E" },
        { "dribbble.com", "#EA4C89" },
    };

    public FaviconService(ILoggingService logger)
    {
        _logger = logger;
        
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _cacheDir = Path.Combine(appDataPath, "SemaphURL", "favicons");
        Directory.CreateDirectory(_cacheDir);

        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(5)
        };
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
    }

    public async Task<ImageSource?> GetFaviconAsync(string url)
    {
        try
        {
            var domain = GetDomainFromUrl(url);
            if (string.IsNullOrEmpty(domain))
                return null;

            // Check memory cache first
            if (_memoryCache.TryGetValue(domain, out var cached))
                return cached;

            // Check file cache
            var cachedPath = GetFaviconCachePath(domain);
            if (File.Exists(cachedPath))
            {
                var image = LoadImageFromFile(cachedPath);
                if (image != null)
                {
                    _memoryCache[domain] = image;
                    return image;
                }
            }

            // Try to download favicon
            var favicon = await DownloadFaviconAsync(domain);
            if (favicon != null)
            {
                _memoryCache[domain] = favicon;
                return favicon;
            }

            // Fallback to color-based icon for popular sites
            var fallbackIcon = CreateFallbackIcon(domain);
            _memoryCache[domain] = fallbackIcon;
            return fallbackIcon;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to get favicon for {url}", ex);
            return CreateFallbackIcon(GetDomainFromUrl(url) ?? "unknown");
        }
    }

    public ImageSource? GetCachedFavicon(string url)
    {
        var domain = GetDomainFromUrl(url);
        if (string.IsNullOrEmpty(domain))
            return null;

        if (_memoryCache.TryGetValue(domain, out var cached))
            return cached;

        var cachedPath = GetFaviconCachePath(domain);
        if (File.Exists(cachedPath))
        {
            var image = LoadImageFromFile(cachedPath);
            if (image != null)
            {
                _memoryCache[domain] = image;
                return image;
            }
        }

        return CreateFallbackIcon(domain);
    }

    public string GetFaviconCachePath(string domain)
    {
        var safeDomain = string.Join("_", domain.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(_cacheDir, $"{safeDomain}.png");
    }

    private async Task<ImageSource?> DownloadFaviconAsync(string domain)
    {
        var urls = new[]
        {
            $"https://www.google.com/s2/favicons?domain={domain}&sz=64",
            $"https://{domain}/favicon.ico",
            $"https://{domain}/favicon.png",
            $"https://www.{domain}/favicon.ico",
        };

        foreach (var faviconUrl in urls)
        {
            try
            {
                var response = await _httpClient.GetAsync(faviconUrl);
                if (response.IsSuccessStatusCode)
                {
                    var bytes = await response.Content.ReadAsByteArrayAsync();
                    if (bytes.Length > 0)
                    {
                        var image = LoadImageFromBytes(bytes);
                        if (image != null)
                        {
                            // Save to cache
                            await SaveToCacheAsync(domain, bytes);
                            return image;
                        }
                    }
                }
            }
            catch
            {
                // Try next URL
            }
        }

        return null;
    }

    private async Task SaveToCacheAsync(string domain, byte[] imageBytes)
    {
        try
        {
            var cachePath = GetFaviconCachePath(domain);
            await File.WriteAllBytesAsync(cachePath, imageBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to cache favicon for {domain}", ex);
        }
    }

    private static ImageSource? LoadImageFromBytes(byte[] bytes)
    {
        try
        {
            using var stream = new MemoryStream(bytes);
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch
        {
            return null;
        }
    }

    private static ImageSource? LoadImageFromFile(string path)
    {
        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(path, UriKind.Absolute);
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch
        {
            return null;
        }
    }

    private static string? GetDomainFromUrl(string url)
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
            return null;
        }
    }

    private static ImageSource CreateFallbackIcon(string domain)
    {
        // Get color for popular sites or generate from domain
        var color = GetColorForDomain(domain);
        
        // Create a simple colored square with first letter
        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            var rect = new System.Windows.Rect(0, 0, 48, 48);
            context.DrawRoundedRectangle(
                new SolidColorBrush(color),
                null,
                rect,
                8, 8);

            // Draw first letter
            var letter = domain.Length > 0 ? char.ToUpper(domain[0]).ToString() : "?";
            var formattedText = new FormattedText(
                letter,
                System.Globalization.CultureInfo.CurrentCulture,
                System.Windows.FlowDirection.LeftToRight,
                new Typeface("Segoe UI"),
                28,
                System.Windows.Media.Brushes.White,
                96);

            var x = (48 - formattedText.Width) / 2;
            var y = (48 - formattedText.Height) / 2;
            context.DrawText(formattedText, new System.Windows.Point(x, y));
        }

        var bitmap = new RenderTargetBitmap(48, 48, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        return bitmap;
    }

    private static Color GetColorForDomain(string domain)
    {
        // Check popular sites first
        foreach (var (site, hexColor) in PopularSiteFallbacks)
        {
            if (domain.Contains(site, StringComparison.OrdinalIgnoreCase))
            {
                return (Color)ColorConverter.ConvertFromString(hexColor);
            }
        }

        // Generate color from domain hash
        var hash = domain.GetHashCode();
        var hue = Math.Abs(hash % 360);
        return HslToRgb(hue, 0.65, 0.45);
    }

    private static Color HslToRgb(double h, double s, double l)
    {
        double r, g, b;

        if (s == 0)
        {
            r = g = b = l;
        }
        else
        {
            var q = l < 0.5 ? l * (1 + s) : l + s - l * s;
            var p = 2 * l - q;
            r = HueToRgb(p, q, h / 360.0 + 1.0 / 3.0);
            g = HueToRgb(p, q, h / 360.0);
            b = HueToRgb(p, q, h / 360.0 - 1.0 / 3.0);
        }

        return Color.FromRgb((byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
    }

    private static double HueToRgb(double p, double q, double t)
    {
        if (t < 0) t += 1;
        if (t > 1) t -= 1;
        if (t < 1.0 / 6.0) return p + (q - p) * 6 * t;
        if (t < 1.0 / 2.0) return q;
        if (t < 2.0 / 3.0) return p + (q - p) * (2.0 / 3.0 - t) * 6;
        return p;
    }
}

