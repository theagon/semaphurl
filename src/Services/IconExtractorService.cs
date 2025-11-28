using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SemaphURL.Services;

public interface IIconExtractorService
{
    ImageSource? GetIconFromExe(string exePath);
    ImageSource? GetIconFromExe(string exePath, int iconIndex);
}

/// <summary>
/// Service for extracting icons from executable files
/// </summary>
public class IconExtractorService : IIconExtractorService
{
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    private readonly Dictionary<string, ImageSource?> _iconCache = new(StringComparer.OrdinalIgnoreCase);

    public ImageSource? GetIconFromExe(string exePath)
    {
        return GetIconFromExe(exePath, 0);
    }

    public ImageSource? GetIconFromExe(string exePath, int iconIndex)
    {
        if (string.IsNullOrEmpty(exePath))
            return null;

        var cacheKey = $"{exePath}:{iconIndex}";
        
        if (_iconCache.TryGetValue(cacheKey, out var cached))
            return cached;

        ImageSource? result = null;

        try
        {
            if (!File.Exists(exePath))
                return null;

            var hIcon = ExtractIcon(IntPtr.Zero, exePath, iconIndex);
            
            if (hIcon != IntPtr.Zero && hIcon.ToInt32() != 1)
            {
                try
                {
                    using var icon = Icon.FromHandle(hIcon);
                    result = Imaging.CreateBitmapSourceFromHIcon(
                        icon.Handle,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());
                    
                    // Freeze for cross-thread access
                    result.Freeze();
                }
                finally
                {
                    DestroyIcon(hIcon);
                }
            }
        }
        catch
        {
            // Ignore icon extraction errors
        }

        _iconCache[cacheKey] = result;
        return result;
    }
}

