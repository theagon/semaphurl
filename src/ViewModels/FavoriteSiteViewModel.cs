using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SemaphURL.Models;

namespace SemaphURL.ViewModels;

/// <summary>
/// ViewModel for a single favorite site in the grid
/// </summary>
public partial class FavoriteSiteViewModel : ObservableObject
{
    private readonly FavoriteSite _site;
    private readonly Action<FavoriteSiteViewModel>? _onOpen;
    private readonly Action<FavoriteSiteViewModel>? _onEdit;
    private readonly Action<FavoriteSiteViewModel>? _onDelete;

    public Guid Id => _site.Id;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _url = string.Empty;

    [ObservableProperty]
    private ImageSource? _icon;

    [ObservableProperty]
    private bool _isHovered;

    [ObservableProperty]
    private bool _isLoading = true;

    [ObservableProperty]
    private int _order;

    [ObservableProperty]
    private string _targetBrowserName = "Default Browser";

    [ObservableProperty]
    private string? _browserPath;

    /// <summary>
    /// True if a custom browser is set for this site (overrides routing rules)
    /// </summary>
    public bool UsesCustomBrowser => !string.IsNullOrEmpty(BrowserPath);

    public FavoriteSiteViewModel(
        FavoriteSite site,
        ImageSource? icon = null,
        Action<FavoriteSiteViewModel>? onOpen = null,
        Action<FavoriteSiteViewModel>? onEdit = null,
        Action<FavoriteSiteViewModel>? onDelete = null)
    {
        _site = site;
        _onOpen = onOpen;
        _onEdit = onEdit;
        _onDelete = onDelete;

        Name = site.Name;
        Url = site.Url;
        Icon = icon;
        Order = site.Order;
        BrowserPath = site.BrowserPath;
    }

    [RelayCommand]
    private void Open()
    {
        _onOpen?.Invoke(this);
    }

    [RelayCommand]
    private void Edit()
    {
        _onEdit?.Invoke(this);
    }

    [RelayCommand]
    private void Delete()
    {
        _onDelete?.Invoke(this);
    }

    public FavoriteSite ToModel() => new()
    {
        Id = _site.Id,
        Name = Name,
        Url = Url,
        IconPath = _site.IconPath,
        BrowserPath = BrowserPath,
        Order = Order
    };

    public void UpdateIcon(ImageSource? icon)
    {
        Icon = icon;
        IsLoading = false;
    }
}

