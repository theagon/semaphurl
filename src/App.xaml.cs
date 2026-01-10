using System.Windows;
using H.NotifyIcon;
using SemaphURL.Services;
using SemaphURL.ViewModels;
using SemaphURL.Views;
using Microsoft.Extensions.DependencyInjection;

namespace SemaphURL;

public partial class App : Application
{
    private readonly ServiceProvider _serviceProvider;
    private readonly ISingleInstanceService _singleInstance;
    private TaskbarIcon? _trayIcon;
    private MainWindow? _mainWindow;
    private FavoriteSitesWindow? _favoriteSitesWindow;
    private UrlHistoryWindow? _urlHistoryWindow;
    private CancellationTokenSource? _cts;
    private IHotkeyService? _hotkeyService;

    public static App Instance => (App)Current;
    public IServiceProvider Services => _serviceProvider;

    public App()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
        _singleInstance = _serviceProvider.GetRequiredService<ISingleInstanceService>();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Services
        services.AddSingleton<ISingleInstanceService, SingleInstanceService>();
        services.AddSingleton<ILoggingService, LoggingService>();
        services.AddSingleton<IConfigurationService, ConfigurationService>();
        services.AddSingleton<IRoutingService, RoutingService>();
        services.AddSingleton<IBrowserRegistrationService, BrowserRegistrationService>();
        services.AddSingleton<IBrowserDiscoveryService, BrowserDiscoveryService>();
        services.AddSingleton<IIconExtractorService, IconExtractorService>();
        services.AddSingleton<IStartupService, StartupService>();
        services.AddSingleton<IFaviconService, FaviconService>();
        services.AddSingleton<IHotkeyService, HotkeyService>();
        services.AddSingleton<IUrlHistoryService, UrlHistoryService>();
        services.AddSingleton<IClipboardService, ClipboardService>();

        // ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<FavoriteSitesViewModel>();
        services.AddTransient<UrlHistoryViewModel>();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var logger = _serviceProvider.GetRequiredService<ILoggingService>();
        var config = _serviceProvider.GetRequiredService<IConfigurationService>();
        var routing = _serviceProvider.GetRequiredService<IRoutingService>();
        var registration = _serviceProvider.GetRequiredService<IBrowserRegistrationService>();

        // Handle --register argument (when restarted as admin)
        if (e.Args.Contains("--register"))
        {
            logger.LogInfo("Running registration as admin");
            registration.Register();
            registration.OpenDefaultAppsSettings();
            Shutdown();
            return;
        }

        // Load configuration
        await config.LoadAsync();

        // Load URL history
        var history = _serviceProvider.GetRequiredService<IUrlHistoryService>();
        await history.LoadAsync();

        // Check for URL argument
        var urlArg = e.Args.FirstOrDefault(a => 
            a.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || 
            a.StartsWith("https://", StringComparison.OrdinalIgnoreCase));

        if (!_singleInstance.IsFirstInstance)
        {
            // Another instance is running
            if (!string.IsNullOrEmpty(urlArg))
            {
                // Send URL to running instance
                await _singleInstance.SendUrlToRunningInstanceAsync(urlArg);
                logger.LogInfo($"Sent URL to running instance: {urlArg}");
            }
            else
            {
                // Just bring the existing window to front via message
                await _singleInstance.SendUrlToRunningInstanceAsync("__SHOW_WINDOW__");
            }
            
            Shutdown();
            return;
        }

        // We are the first instance
        logger.LogInfo("SemaphURL started");

        // Setup tray icon
        SetupTrayIcon();

        // Show startup notification
        ShowStartupNotification();

        // Setup global hotkeys
        _hotkeyService = _serviceProvider.GetRequiredService<IHotkeyService>();
        _hotkeyService.FavoriteSitesHotkeyPressed += OnFavoriteSitesHotkeyPressed;
        _hotkeyService.ClipboardUrlHotkeyPressed += OnClipboardUrlHotkeyPressed;
        _hotkeyService.RegisterAll(config.Config.FavoriteSitesHotkey, config.Config.ClipboardUrlHotkey);

        // Start listening for URLs from other instances
        _cts = new CancellationTokenSource();
        _singleInstance.UrlReceived += OnUrlReceived;
        _ = Task.Run(() => _singleInstance.StartListeningAsync(_cts.Token));

        // Process URL if provided
        if (!string.IsNullOrEmpty(urlArg))
        {
            await ProcessUrlAsync(urlArg);
        }

        // Check if started with --minimized argument (from Windows startup)
        var startMinimized = e.Args.Contains("--minimized") || config.Config.StartMinimized;

        // Show or hide window based on settings
        if (!startMinimized && string.IsNullOrEmpty(urlArg))
        {
            ShowMainWindow();
        }
    }

    private void SetupTrayIcon()
    {
        var logger = _serviceProvider.GetRequiredService<ILoggingService>();
        
        try
        {
            // Get TaskbarIcon from XAML resources
            _trayIcon = (TaskbarIcon)FindResource("TrayIcon");
            _trayIcon.TrayLeftMouseUp += (_, _) => ShowMainWindow();
            
            // Force the icon to be visible
            _trayIcon.Visibility = System.Windows.Visibility.Visible;
            _trayIcon.ForceCreate();
            
            logger.LogInfo("Tray icon initialized from XAML resources");
        }
        catch (Exception ex)
        {
            logger.LogError("Failed to setup tray icon from resources, creating fallback", ex);
            
            // Fallback: create programmatically
            var icon = LoadIconFromResource();
            _trayIcon = new TaskbarIcon
            {
                Icon = icon,
                ToolTipText = "SemaphURL - Click to open settings",
                ContextMenu = CreateTrayContextMenu(),
                NoLeftClickDelay = true,
                Visibility = System.Windows.Visibility.Visible
            };
            _trayIcon.TrayLeftMouseUp += (_, _) => ShowMainWindow();
            _trayIcon.ForceCreate();
        }
    }

    private void ShowStartupNotification()
    {
        if (_trayIcon == null) return;

        try
        {
            // Use Windows toast notification via tray icon
            _trayIcon.ShowNotification(
                title: "SemaphURL",
                message: "Running in system tray. Press Ctrl+Space for Favorite Sites.");
        }
        catch (Exception ex)
        {
            var logger = _serviceProvider.GetRequiredService<ILoggingService>();
            logger.LogError("Failed to show startup notification", ex);
        }
    }

    private static System.Drawing.Icon LoadIconFromResource()
    {
        try
        {
            var uri = new Uri("pack://application:,,,/Assets/icon.ico");
            var resourceStream = GetResourceStream(uri);
            if (resourceStream != null)
            {
                return new System.Drawing.Icon(resourceStream.Stream);
            }
        }
        catch { }

        // Fallback: create a simple icon programmatically
        return CreateDefaultIcon();
    }

    private static System.Drawing.Icon CreateDefaultIcon()
    {
        using var bitmap = new System.Drawing.Bitmap(32, 32);
        using var g = System.Drawing.Graphics.FromImage(bitmap);
        
        g.Clear(System.Drawing.Color.FromArgb(123, 104, 238)); // Medium Slate Blue
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        
        // Draw a simple arrow/router symbol
        using var pen = new System.Drawing.Pen(System.Drawing.Color.White, 2);
        g.DrawLine(pen, 8, 16, 24, 16);
        g.DrawLine(pen, 18, 10, 24, 16);
        g.DrawLine(pen, 18, 22, 24, 16);
        
        return System.Drawing.Icon.FromHandle(bitmap.GetHicon());
    }

    // XAML tray menu event handlers
    private void TrayMenu_FavoriteSites_Click(object sender, RoutedEventArgs e) => ShowFavoriteSites();
    private void TrayMenu_UrlHistory_Click(object sender, RoutedEventArgs e) => ShowUrlHistory();
    private void TrayMenu_OpenSettings_Click(object sender, RoutedEventArgs e) => ShowMainWindow();
    private void TrayMenu_Exit_Click(object sender, RoutedEventArgs e) => ExitApplication();

    private void TrayContextMenu_Opened(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.ContextMenu menu)
            return;

        // Find the Recent URLs menu item
        var recentMenuItem = menu.Items
            .OfType<System.Windows.Controls.MenuItem>()
            .FirstOrDefault(m => m.Header?.ToString() == "Recent URLs");

        if (recentMenuItem == null)
            return;

        // Clear existing items
        recentMenuItem.Items.Clear();

        // Get recent URLs from history service
        var historyService = _serviceProvider.GetRequiredService<IUrlHistoryService>();
        var recentUrls = historyService.GetRecentUrls(5).ToList();

        if (recentUrls.Count == 0)
        {
            var emptyItem = new System.Windows.Controls.MenuItem
            {
                Header = "(No recent URLs)",
                IsEnabled = false
            };
            recentMenuItem.Items.Add(emptyItem);
        }
        else
        {
            foreach (var entry in recentUrls)
            {
                // Truncate URL for display (max 50 chars)
                var displayUrl = entry.Url.Length > 50
                    ? entry.Url[..47] + "..."
                    : entry.Url;

                var urlItem = new System.Windows.Controls.MenuItem
                {
                    Header = displayUrl,
                    ToolTip = entry.Url
                };

                var url = entry.Url; // Capture for closure
                urlItem.Click += (_, _) => _ = ProcessUrlAsync(url);
                recentMenuItem.Items.Add(urlItem);
            }
        }
    }

    private System.Windows.Controls.ContextMenu CreateTrayContextMenu()
    {
        var menu = new System.Windows.Controls.ContextMenu();

        var favoritesItem = new System.Windows.Controls.MenuItem { Header = "Favorite Sites (Ctrl+Space)" };
        favoritesItem.Click += (_, _) => ShowFavoriteSites();
        menu.Items.Add(favoritesItem);

        menu.Items.Add(new System.Windows.Controls.Separator());

        var openItem = new System.Windows.Controls.MenuItem { Header = "Open Settings" };
        openItem.Click += (_, _) => ShowMainWindow();
        menu.Items.Add(openItem);

        menu.Items.Add(new System.Windows.Controls.Separator());

        var exitItem = new System.Windows.Controls.MenuItem { Header = "Exit" };
        exitItem.Click += (_, _) => ExitApplication();
        menu.Items.Add(exitItem);

        return menu;
    }

    private async void OnUrlReceived(string url)
    {
        if (url == "__SHOW_WINDOW__")
        {
            Dispatcher.Invoke(ShowMainWindow);
            return;
        }

        if (url == "__SHOW_FAVORITES__")
        {
            Dispatcher.Invoke(ShowFavoriteSites);
            return;
        }

        await ProcessUrlAsync(url);
    }

    private void OnFavoriteSitesHotkeyPressed()
    {
        Dispatcher.Invoke(ToggleFavoriteSites);
    }

    private void OnClipboardUrlHotkeyPressed()
    {
        Dispatcher.Invoke(HandleClipboardUrl);
    }

    private void HandleClipboardUrl()
    {
        var clipboardService = _serviceProvider.GetRequiredService<IClipboardService>();
        var url = clipboardService.GetClipboardUrl();

        if (string.IsNullOrEmpty(url))
        {
            _trayIcon?.ShowNotification(
                "SemaphURL",
                "No URL found in clipboard.");
            return;
        }

        // Show notification and open URL
        _trayIcon?.ShowNotification(
            "SemaphURL",
            $"Opening: {url}");

        _ = ProcessUrlAsync(url);
    }

    public void ToggleFavoriteSites()
    {
        if (_favoriteSitesWindow != null && _favoriteSitesWindow.IsVisible)
        {
            _favoriteSitesWindow.Hide();
        }
        else
        {
            ShowFavoriteSites();
        }
    }

    public void ShowFavoriteSites()
    {
        if (_favoriteSitesWindow == null || !_favoriteSitesWindow.IsLoaded)
        {
            _favoriteSitesWindow = new FavoriteSitesWindow
            {
                DataContext = _serviceProvider.GetRequiredService<FavoriteSitesViewModel>()
            };
            _favoriteSitesWindow.Closed += (_, _) => _favoriteSitesWindow = null;
        }

        _favoriteSitesWindow.Show();
        _favoriteSitesWindow.Activate();
    }

    public void HideFavoriteSites()
    {
        _favoriteSitesWindow?.Hide();
    }

    public void ShowUrlHistory()
    {
        if (_urlHistoryWindow == null || !_urlHistoryWindow.IsLoaded)
        {
            _urlHistoryWindow = new UrlHistoryWindow
            {
                DataContext = _serviceProvider.GetRequiredService<UrlHistoryViewModel>()
            };
            _urlHistoryWindow.Closed += (_, _) => _urlHistoryWindow = null;
        }

        _urlHistoryWindow.Show();
        _urlHistoryWindow.Activate();
    }

    public void HideUrlHistory()
    {
        _urlHistoryWindow?.Hide();
    }

    private async Task ProcessUrlAsync(string url)
    {
        var routing = _serviceProvider.GetRequiredService<IRoutingService>();

        var result = routing.Route(url);
        await routing.ExecuteRoutingAsync(result);
    }

    public void ShowMainWindow()
    {
        if (_mainWindow == null || !_mainWindow.IsLoaded)
        {
            _mainWindow = new MainWindow
            {
                DataContext = _serviceProvider.GetRequiredService<MainViewModel>()
            };
            _mainWindow.Closed += (_, _) => _mainWindow = null;
        }

        _mainWindow.Show();
        _mainWindow.Activate();
        
        if (_mainWindow.WindowState == WindowState.Minimized)
        {
            _mainWindow.WindowState = WindowState.Normal;
        }
    }

    public void MinimizeToTray()
    {
        _mainWindow?.Hide();
    }

    public void ExitApplication()
    {
        _cts?.Cancel();
        _hotkeyService?.UnregisterAll();
        _trayIcon?.Dispose();
        _singleInstance.Dispose();
        _serviceProvider.Dispose();
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        var logger = _serviceProvider.GetService<ILoggingService>();
        logger?.LogInfo("SemaphURL stopped");
        base.OnExit(e);
    }
}

