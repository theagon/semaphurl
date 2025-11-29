using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SemaphURL.Models;
using SemaphURL.Services;

namespace SemaphURL.ViewModels;

/// <summary>
/// ViewModel for the Favorite Sites popup window
/// </summary>
public partial class FavoriteSitesViewModel : ObservableObject
{
    private readonly IConfigurationService _config;
    private readonly IRoutingService _routing;
    private readonly IFaviconService _favicon;
    private readonly ILoggingService _logger;
    private readonly IBrowserDiscoveryService _browserDiscovery;

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private bool _isAddingNew;

    [ObservableProperty]
    private string _editName = string.Empty;

    [ObservableProperty]
    private string _editUrl = string.Empty;

    [ObservableProperty]
    private FavoriteSiteViewModel? _editingSite;

    [ObservableProperty]
    private BrowserOption? _selectedBrowser;

    public ObservableCollection<FavoriteSiteViewModel> Sites { get; } = [];

    /// <summary>
    /// List of browser options for the dropdown (includes "Auto" option)
    /// </summary>
    public ObservableCollection<BrowserOption> BrowserOptions { get; } = [];

    public FavoriteSitesViewModel(
        IConfigurationService config,
        IRoutingService routing,
        IFaviconService favicon,
        ILoggingService logger,
        IBrowserDiscoveryService browserDiscovery)
    {
        _config = config;
        _routing = routing;
        _favicon = favicon;
        _logger = logger;
        _browserDiscovery = browserDiscovery;

        LoadBrowserOptions();
        LoadSites();
    }

    private void LoadBrowserOptions()
    {
        BrowserOptions.Clear();
        
        // Add "Auto" option first
        BrowserOptions.Add(new BrowserOption("Use routing rules (auto)", null));
        
        // Add installed browsers
        foreach (var browser in _browserDiscovery.GetInstalledBrowsers())
        {
            BrowserOptions.Add(new BrowserOption(browser.Name, browser.ExePath));
        }
    }

    public async void LoadSites()
    {
        Sites.Clear();

        // Initialize default sites if empty
        if (_config.Config.FavoriteSites.Count == 0)
        {
            InitializeDefaultSites();
            await _config.SaveAsync();
        }

        // First, add all sites with loading state (skeleton)
        var siteViewModels = new List<FavoriteSiteViewModel>();
        foreach (var site in _config.Config.FavoriteSites.OrderBy(s => s.Order))
        {
            var vm = new FavoriteSiteViewModel(
                site,
                icon: null, // No icon yet - will show skeleton
                onOpen: OpenSite,
                onEdit: StartEditSite,
                onDelete: DeleteSite);
            Sites.Add(vm);
            siteViewModels.Add(vm);
        }

        // Then load icons asynchronously
        foreach (var vm in siteViewModels)
        {
            _ = LoadIconAsync(vm);
        }
    }

    private async Task LoadIconAsync(FavoriteSiteViewModel vm)
    {
        try
        {
            // Load icon
            var icon = await _favicon.GetFaviconAsync(vm.Url);
            vm.UpdateIcon(icon);
            
            // Resolve target browser - use custom browser if set, otherwise routing rules
            if (vm.UsesCustomBrowser)
            {
                vm.TargetBrowserName = GetBrowserDisplayName(vm.BrowserPath) + " (custom)";
            }
            else
            {
                var routingResult = _routing.Route(vm.Url);
                vm.TargetBrowserName = GetBrowserDisplayName(routingResult.BrowserPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to load favicon for {vm.Url}", ex);
            vm.UpdateIcon(null); // This will also set IsLoading = false
        }
    }

    private string GetBrowserDisplayName(string? browserPath)
    {
        if (string.IsNullOrEmpty(browserPath))
            return "Default Browser";

        try
        {
            var fileName = System.IO.Path.GetFileNameWithoutExtension(browserPath);
            return fileName switch
            {
                "chrome" => "Google Chrome",
                "msedge" => "Microsoft Edge",
                "firefox" => "Mozilla Firefox",
                "brave" => "Brave",
                "opera" => "Opera",
                "vivaldi" => "Vivaldi",
                "browser" => "Yandex Browser",
                _ => fileName
            };
        }
        catch
        {
            return "Browser";
        }
    }

    private void InitializeDefaultSites()
    {
        var defaults = new[]
        {
            new FavoriteSite("YouTube", "https://youtube.com", 0),
            new FavoriteSite("GitHub", "https://github.com", 1),
            new FavoriteSite("Reddit", "https://reddit.com", 2),
            new FavoriteSite("X (Twitter)", "https://x.com", 3),
            new FavoriteSite("Facebook", "https://facebook.com", 4),
            new FavoriteSite("Instagram", "https://instagram.com", 5),
            new FavoriteSite("LinkedIn", "https://linkedin.com", 6),
            new FavoriteSite("Stack Overflow", "https://stackoverflow.com", 7),
        };

        _config.Config.FavoriteSites = defaults.ToList();
    }

    private void OpenSite(FavoriteSiteViewModel siteVm)
    {
        try
        {
            if (siteVm.UsesCustomBrowser)
            {
                // Direct open with selected browser (bypass routing)
                var startInfo = new ProcessStartInfo
                {
                    FileName = siteVm.BrowserPath,
                    Arguments = $"\"{siteVm.Url}\"",
                    UseShellExecute = true
                };
                Process.Start(startInfo);
                _logger.LogInfo($"Opened favorite site with custom browser: {siteVm.Name} -> {siteVm.Url} ({siteVm.BrowserPath})");
            }
            else
            {
                // Use routing service
                var result = _routing.Route(siteVm.Url);
                _ = _routing.ExecuteRoutingAsync(result);
                _logger.LogInfo($"Opened favorite site via routing: {siteVm.Name} -> {siteVm.Url}");
            }
            
            // Close the window after opening
            App.Instance.HideFavoriteSites();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to open favorite site: {siteVm.Url}", ex);
        }
    }

    private void StartEditSite(FavoriteSiteViewModel siteVm)
    {
        EditingSite = siteVm;
        EditName = siteVm.Name;
        EditUrl = siteVm.Url;
        
        // Select current browser in dropdown
        SelectedBrowser = BrowserOptions.FirstOrDefault(b => 
            b.Path?.Equals(siteVm.BrowserPath, StringComparison.OrdinalIgnoreCase) == true)
            ?? BrowserOptions.First(); // Default to "Auto"
        
        IsAddingNew = false;
        IsEditing = true;
    }

    [RelayCommand]
    private void StartAddNew()
    {
        EditingSite = null;
        EditName = string.Empty;
        EditUrl = string.Empty;
        SelectedBrowser = BrowserOptions.First(); // Default to "Auto"
        IsAddingNew = true;
        IsEditing = true;
    }

    [RelayCommand]
    private async Task ConfirmEdit()
    {
        if (string.IsNullOrWhiteSpace(EditName) || string.IsNullOrWhiteSpace(EditUrl))
            return;

        // Ensure URL has protocol
        var url = EditUrl.Trim();
        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            url = "https://" + url;
        }

        // Get selected browser path (null for "Auto")
        var browserPath = SelectedBrowser?.Path;

        if (IsAddingNew)
        {
            // Add new site
            var newSite = new FavoriteSite(EditName.Trim(), url, Sites.Count)
            {
                BrowserPath = browserPath
            };
            _config.Config.FavoriteSites.Add(newSite);

            // Add with loading state first
            var vm = new FavoriteSiteViewModel(
                newSite,
                icon: null,
                onOpen: OpenSite,
                onEdit: StartEditSite,
                onDelete: DeleteSite);
            Sites.Add(vm);
            
            // Load icon asynchronously
            _ = LoadIconAsync(vm);
        }
        else if (EditingSite != null)
        {
            var urlChanged = !EditingSite.Url.Equals(url, StringComparison.OrdinalIgnoreCase);
            var browserChanged = EditingSite.BrowserPath != browserPath;
            
            // Update existing site
            EditingSite.Name = EditName.Trim();
            EditingSite.Url = url;
            EditingSite.BrowserPath = browserPath;

            // Update the model in config
            var configSite = _config.Config.FavoriteSites.FirstOrDefault(s => s.Id == EditingSite.Id);
            if (configSite != null)
            {
                configSite.Name = EditName.Trim();
                configSite.Url = url;
                configSite.BrowserPath = browserPath;
            }

            // Update display if URL or browser changed
            if (urlChanged || browserChanged)
            {
                EditingSite.IsLoading = true;
                _ = LoadIconAsync(EditingSite);
            }
        }

        await _config.SaveAsync();
        CancelEdit();
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
        IsAddingNew = false;
        EditingSite = null;
        EditName = string.Empty;
        EditUrl = string.Empty;
        SelectedBrowser = null;
    }

    private async void DeleteSite(FavoriteSiteViewModel siteVm)
    {
        Sites.Remove(siteVm);
        _config.Config.FavoriteSites.RemoveAll(s => s.Id == siteVm.Id);

        // Reorder remaining sites
        for (int i = 0; i < Sites.Count; i++)
        {
            Sites[i].Order = i;
            var configSite = _config.Config.FavoriteSites.FirstOrDefault(s => s.Id == Sites[i].Id);
            if (configSite != null)
                configSite.Order = i;
        }

        await _config.SaveAsync();
    }

    [RelayCommand]
    private void Close()
    {
        App.Instance.HideFavoriteSites();
    }
}

/// <summary>
/// Represents a browser option in the dropdown
/// </summary>
public class BrowserOption
{
    public string Name { get; }
    public string? Path { get; }

    public BrowserOption(string name, string? path)
    {
        Name = name;
        Path = path;
    }

    public override string ToString() => Name;
}

