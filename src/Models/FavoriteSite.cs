namespace SemaphURL.Models;

/// <summary>
/// Represents a favorite site for quick access
/// </summary>
public class FavoriteSite
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? IconPath { get; set; }
    public string? BrowserPath { get; set; }  // null = use routing rules
    public int Order { get; set; }

    public FavoriteSite() { }

    public FavoriteSite(string name, string url, int order = 0)
    {
        Name = name;
        Url = url;
        Order = order;
    }

    public FavoriteSite Clone() => new()
    {
        Id = Id,
        Name = Name,
        Url = Url,
        IconPath = IconPath,
        BrowserPath = BrowserPath,
        Order = Order
    };
}

