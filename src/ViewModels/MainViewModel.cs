using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SemaphURL.Models;
using SemaphURL.Services;
using Microsoft.Win32;

namespace SemaphURL.ViewModels;

/// <summary>
/// Main ViewModel for the settings window
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly IConfigurationService _config;
    private readonly IRoutingService _routing;
    private readonly ILoggingService _logger;
    private readonly IBrowserRegistrationService _registration;
    private readonly IBrowserDiscoveryService _browserDiscovery;
    private readonly IIconExtractorService _iconExtractor;
    private readonly IStartupService _startup;
    private readonly IHotkeyService _hotkey;

    [ObservableProperty]
    private string _defaultBrowserPath = string.Empty;

    [ObservableProperty]
    private string _defaultBrowserArguments = "\"{url}\"";

    [ObservableProperty]
    private bool _minimizeToTrayOnClose = true;

    [ObservableProperty]
    private bool _startMinimized;

    [ObservableProperty]
    private bool _focusBrowserAfterRouting = true;

    [ObservableProperty]
    private bool _startWithWindows;

    [ObservableProperty]
    private string _favoriteSitesHotkey = "Ctrl+Space";

    [ObservableProperty]
    private string _clipboardUrlHotkey = "Ctrl+Shift+Space";

    [ObservableProperty]
    private string _testUrl = string.Empty;

    [ObservableProperty]
    private string _testResult = string.Empty;

    [ObservableProperty]
    private RuleViewModel? _selectedRule;

    [ObservableProperty]
    private bool _hasUnsavedChanges;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private bool _isRegistered;

    [ObservableProperty]
    private bool _isRunningAsAdmin;

    // For new rule dialog
    [ObservableProperty]
    private bool _isAddingRule;

    [ObservableProperty]
    private InstalledBrowser? _newRuleBrowser;

    [ObservableProperty]
    private string _newRuleName = "New Rule";

    [ObservableProperty]
    private PatternType _newRulePatternType = PatternType.DomainContains;

    [ObservableProperty]
    private string _newRulePattern = string.Empty;

    // For default browser selection display
    [ObservableProperty]
    private InstalledBrowser? _selectedDefaultBrowser;

    // Developer Mode
    [ObservableProperty]
    private bool _developerMode;

    // Collections
    public ObservableCollection<RuleViewModel> Rules { get; } = [];
    public ObservableCollection<BrowserGroupViewModel> BrowserGroups { get; } = [];

    public IReadOnlyList<string> AvailablePlaceholders => PlaceholderResolver.GetAvailablePlaceholders();
    public IReadOnlyList<InstalledBrowser> InstalledBrowsers => _browserDiscovery.GetInstalledBrowsers();
    
    /// <summary>
    /// All pattern types (used internally)
    /// </summary>
    public static IEnumerable<PatternType> PatternTypes => Enum.GetValues<PatternType>();
    
    /// <summary>
    /// Pattern types available based on Developer Mode setting
    /// </summary>
    public IEnumerable<PatternType> AvailablePatternTypes => DeveloperMode
        ? Enum.GetValues<PatternType>()
        : new[] { PatternType.DomainContains, PatternType.DomainEquals, PatternType.UrlContains };

    /// <summary>
    /// Window title changes based on Developer Mode
    /// </summary>
    public string WindowTitle => DeveloperMode ? "SemaphURL Settings [Developer]" : "SemaphURL Settings";

    public MainViewModel(
        IConfigurationService config, 
        IRoutingService routing, 
        ILoggingService logger, 
        IBrowserRegistrationService registration,
        IBrowserDiscoveryService browserDiscovery,
        IIconExtractorService iconExtractor,
        IStartupService startup,
        IHotkeyService hotkey)
    {
        _config = config;
        _routing = routing;
        _logger = logger;
        _registration = registration;
        _browserDiscovery = browserDiscovery;
        _iconExtractor = iconExtractor;
        _startup = startup;
        _hotkey = hotkey;
        
        LoadFromConfig();
        RefreshRegistrationStatus();
    }

    private void LoadFromConfig()
    {
        DefaultBrowserPath = _config.Config.DefaultBrowserPath;
        DefaultBrowserArguments = _config.Config.DefaultBrowserArguments;
        MinimizeToTrayOnClose = _config.Config.MinimizeToTrayOnClose;
        StartMinimized = _config.Config.StartMinimized;
        FocusBrowserAfterRouting = _config.Config.FocusBrowserAfterRouting;
        StartWithWindows = _startup.IsEnabled;
        FavoriteSitesHotkey = _config.Config.FavoriteSitesHotkey;
        ClipboardUrlHotkey = _config.Config.ClipboardUrlHotkey;
        DeveloperMode = _config.Config.DeveloperMode;

        Rules.Clear();
        foreach (var rule in _config.Config.Rules.OrderBy(r => r.Order))
        {
            Rules.Add(new RuleViewModel(rule));
        }

        RefreshBrowserGroups();
        UpdateSelectedDefaultBrowser();
        HasUnsavedChanges = false;
    }

    private void UpdateSelectedDefaultBrowser()
    {
        SelectedDefaultBrowser = InstalledBrowsers.FirstOrDefault(b =>
            b.ExePath.Equals(DefaultBrowserPath, StringComparison.OrdinalIgnoreCase));
    }

    private void RefreshBrowserGroups()
    {
        BrowserGroups.Clear();

        // Group rules by browser path
        var groups = Rules
            .GroupBy(r => r.BrowserPath, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => GetBrowserName(g.Key));

        foreach (var group in groups)
        {
            var browserPath = group.Key;
            var browserName = GetBrowserName(browserPath);
            var icon = _iconExtractor.GetIconFromExe(browserPath);

            var browserGroup = new BrowserGroupViewModel
            {
                BrowserName = browserName,
                BrowserPath = browserPath,
                BrowserIcon = icon
            };

            foreach (var rule in group.OrderBy(r => r.Order))
            {
                browserGroup.Rules.Add(rule);
            }

            BrowserGroups.Add(browserGroup);
        }
    }

    private string GetBrowserName(string browserPath)
    {
        if (string.IsNullOrEmpty(browserPath))
            return "No Browser";

        // Try to find in installed browsers
        var installed = InstalledBrowsers.FirstOrDefault(b => 
            b.ExePath.Equals(browserPath, StringComparison.OrdinalIgnoreCase));
        
        if (installed != null)
            return installed.Name;

        // Fallback to filename
        return Path.GetFileNameWithoutExtension(browserPath);
    }

    public ImageSource? GetBrowserIcon(string browserPath)
    {
        return _iconExtractor.GetIconFromExe(browserPath);
    }

    [RelayCommand]
    private void BrowseDefaultBrowser()
    {
        var path = BrowseForBrowser();
        if (!string.IsNullOrEmpty(path))
        {
            DefaultBrowserPath = path;
            HasUnsavedChanges = true;
        }
    }

    [RelayCommand]
    private void SelectDefaultBrowser(InstalledBrowser? browser)
    {
        if (browser == null) return;
        DefaultBrowserPath = browser.ExePath;
        HasUnsavedChanges = true;
    }

    [RelayCommand]
    private void RefreshBrowsers()
    {
        _browserDiscovery.RefreshBrowserList();
        OnPropertyChanged(nameof(InstalledBrowsers));
        RefreshBrowserGroups();
        UpdateSelectedDefaultBrowser();
        StatusMessage = $"Found {InstalledBrowsers.Count} browsers";
    }

    [RelayCommand]
    private void StartAddingRule()
    {
        NewRuleName = "New Rule";
        NewRulePatternType = PatternType.DomainContains;
        NewRulePattern = string.Empty;
        NewRuleBrowser = InstalledBrowsers.FirstOrDefault();
        IsAddingRule = true;
    }

    [RelayCommand]
    private void CancelAddingRule()
    {
        IsAddingRule = false;
    }

    [RelayCommand]
    private void ConfirmAddRule()
    {
        if (NewRuleBrowser == null)
        {
            StatusMessage = "Please select a browser";
            return;
        }

        var newRule = new RuleViewModel(new RoutingRule
        {
            Name = string.IsNullOrWhiteSpace(NewRuleName) ? "New Rule" : NewRuleName,
            PatternType = NewRulePatternType,
            Pattern = NewRulePattern,
            BrowserPath = NewRuleBrowser.ExePath,
            BrowserArgumentsTemplate = "\"{url}\"",
            Order = Rules.Count
        });

        Rules.Add(newRule);
        SelectedRule = newRule;
        HasUnsavedChanges = true;
        UpdateOrders();
        RefreshBrowserGroups();
        IsAddingRule = false;
        StatusMessage = $"Rule added for {NewRuleBrowser.Name}";
    }

    [RelayCommand]
    private void AddRuleToBrowser(BrowserGroupViewModel? browserGroup)
    {
        if (browserGroup == null) return;

        var browser = InstalledBrowsers.FirstOrDefault(b => 
            b.ExePath.Equals(browserGroup.BrowserPath, StringComparison.OrdinalIgnoreCase));

        if (browser != null)
        {
            NewRuleBrowser = browser;
        }

        NewRuleName = "New Rule";
        NewRulePatternType = PatternType.DomainContains;
        NewRulePattern = string.Empty;
        IsAddingRule = true;
    }

    [RelayCommand]
    private void DeleteRule(RuleViewModel? rule)
    {
        if (rule == null) return;
        
        Rules.Remove(rule);
        HasUnsavedChanges = true;
        UpdateOrders();
        RefreshBrowserGroups();
    }

    [RelayCommand]
    private void MoveRuleUp(RuleViewModel? rule)
    {
        if (rule == null) return;
        
        var index = Rules.IndexOf(rule);
        if (index > 0)
        {
            Rules.Move(index, index - 1);
            HasUnsavedChanges = true;
            UpdateOrders();
        }
    }

    [RelayCommand]
    private void MoveRuleDown(RuleViewModel? rule)
    {
        if (rule == null) return;
        
        var index = Rules.IndexOf(rule);
        if (index < Rules.Count - 1)
        {
            Rules.Move(index, index + 1);
            HasUnsavedChanges = true;
            UpdateOrders();
        }
    }

    [RelayCommand]
    private void DuplicateRule(RuleViewModel? rule)
    {
        if (rule == null) return;
        
        var newRule = new RuleViewModel(new RoutingRule
        {
            Name = rule.Name + " (Copy)",
            PatternType = rule.PatternType,
            Pattern = rule.Pattern,
            BrowserPath = rule.BrowserPath,
            BrowserArgumentsTemplate = rule.BrowserArgumentsTemplate,
            Enabled = rule.Enabled,
            Order = Rules.Count
        });
        
        Rules.Add(newRule);
        SelectedRule = newRule;
        HasUnsavedChanges = true;
        UpdateOrders();
        RefreshBrowserGroups();
    }

    [RelayCommand]
    private void ChangeRuleBrowser(object? parameter)
    {
        if (parameter is object[] args && args.Length == 2 
            && args[0] is RuleViewModel rule 
            && args[1] is InstalledBrowser browser)
        {
            rule.BrowserPath = browser.ExePath;
            HasUnsavedChanges = true;
            RefreshBrowserGroups();
        }
    }

    [RelayCommand]
    private void TestRouting()
    {
        if (string.IsNullOrWhiteSpace(TestUrl))
        {
            TestResult = "Please enter a URL to test";
            return;
        }

        try
        {
            var result = _routing.TestRoute(TestUrl);
            
            var browserName = string.IsNullOrEmpty(result.BrowserPath) 
                ? "None" 
                : GetBrowserName(result.BrowserPath);

            TestResult = $"Rule: {result.GetRuleDescription()}\n" +
                        $"Browser: {browserName}\n" +
                        $"Path: {result.BrowserPath}\n" +
                        $"Arguments: {result.Arguments}";
        }
        catch (Exception ex)
        {
            TestResult = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            // Check if hotkeys changed
            var hotkeysChanged = _config.Config.FavoriteSitesHotkey != FavoriteSitesHotkey ||
                                 _config.Config.ClipboardUrlHotkey != ClipboardUrlHotkey;

            _config.Config.DefaultBrowserPath = DefaultBrowserPath;
            _config.Config.DefaultBrowserArguments = DefaultBrowserArguments;
            _config.Config.MinimizeToTrayOnClose = MinimizeToTrayOnClose;
            _config.Config.StartMinimized = StartMinimized;
            _config.Config.FocusBrowserAfterRouting = FocusBrowserAfterRouting;
            _config.Config.StartWithWindows = StartWithWindows;
            _config.Config.FavoriteSitesHotkey = FavoriteSitesHotkey;
            _config.Config.ClipboardUrlHotkey = ClipboardUrlHotkey;
            _config.Config.DeveloperMode = DeveloperMode;
            _config.Config.Rules = Rules.Select(r => r.ToRule()).ToList();

            // Update Windows startup registration
            if (StartWithWindows)
            {
                _startup.Enable();
            }
            else
            {
                _startup.Disable();
            }

            // Re-register hotkeys if changed
            if (hotkeysChanged)
            {
                _hotkey.ReregisterAll(FavoriteSitesHotkey, ClipboardUrlHotkey);
                _logger.LogInfo($"Hotkeys updated: FavoriteSites={FavoriteSitesHotkey}, ClipboardUrl={ClipboardUrlHotkey}");
            }

            await _config.SaveAsync();
            
            HasUnsavedChanges = false;
            StatusMessage = "Configuration saved";
            _logger.LogInfo("Configuration saved");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Save failed: {ex.Message}";
            _logger.LogError("Failed to save configuration", ex);
        }
    }

    [RelayCommand]
    private void Reload()
    {
        LoadFromConfig();
        StatusMessage = "Configuration reloaded";
    }

    [RelayCommand]
    private void OpenConfigFolder()
    {
        try
        {
            var folder = Path.GetDirectoryName(_config.ConfigPath);
            if (folder != null && Directory.Exists(folder))
            {
                System.Diagnostics.Process.Start("explorer.exe", folder);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to open config folder", ex);
        }
    }

    [RelayCommand]
    private void RegisterAsBrowser()
    {
        try
        {
            if (_registration.IsRunningAsAdmin)
            {
                if (_registration.Register())
                {
                    StatusMessage = "Registered! Now select SemaphURL in Windows Settings";
                    _registration.OpenDefaultAppsSettings();
                }
                else
                {
                    StatusMessage = "Registration failed. Check logs for details.";
                }
            }
            else
            {
                StatusMessage = "Restarting as Administrator...";
                _registration.RestartAsAdmin();
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Registration failed: {ex.Message}";
            _logger.LogError("Failed to register as browser", ex);
        }
        finally
        {
            RefreshRegistrationStatus();
        }
    }

    [RelayCommand]
    private void UnregisterBrowser()
    {
        try
        {
            if (_registration.Unregister())
            {
                StatusMessage = "Browser registration removed";
            }
            else
            {
                StatusMessage = "Unregistration failed. Check logs for details.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Unregistration failed: {ex.Message}";
            _logger.LogError("Failed to unregister browser", ex);
        }
        finally
        {
            RefreshRegistrationStatus();
        }
    }

    [RelayCommand]
    private void OpenDefaultAppsSettings()
    {
        _registration.OpenDefaultAppsSettings();
    }

    [RelayCommand]
    private void OpenFavoriteSites()
    {
        App.Instance.ShowFavoriteSites();
    }

    [RelayCommand]
    private void OpenUrlHistory()
    {
        App.Instance.ShowUrlHistory();
    }

    private void RefreshRegistrationStatus()
    {
        IsRegistered = _registration.IsRegistered;
        IsRunningAsAdmin = _registration.IsRunningAsAdmin;
    }

    private void UpdateOrders()
    {
        for (int i = 0; i < Rules.Count; i++)
        {
            Rules[i].Order = i;
        }
    }

    private static string? BrowseForBrowser()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select Browser Executable",
            Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    [RelayCommand]
    private void ExportSettings()
    {
        try
        {
            var dialog = new SaveFileDialog
            {
                Title = "Export Settings",
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                FileName = $"semaphurl-backup-{DateTime.Now:yyyy-MM-dd}.json",
                DefaultExt = ".json"
            };

            if (dialog.ShowDialog() != true)
                return;

            var exportData = new ExportData
            {
                Version = "1.0",
                ExportDate = DateTime.UtcNow,
                Rules = Rules.Select(r => r.ToRule()).ToList(),
                FavoriteSites = _config.Config.FavoriteSites.Select(s => s.Clone()).ToList(),
                Settings = new ExportSettings
                {
                    DefaultBrowserPath = DefaultBrowserPath,
                    DefaultBrowserArguments = DefaultBrowserArguments,
                    DeveloperMode = DeveloperMode,
                    FavoriteSitesHotkey = FavoriteSitesHotkey,
                    ClipboardUrlHotkey = ClipboardUrlHotkey
                }
            };

            var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            
            File.WriteAllText(dialog.FileName, json);
            
            StatusMessage = $"Settings exported to {Path.GetFileName(dialog.FileName)}";
            _logger.LogInfo($"Settings exported to {dialog.FileName}");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export failed: {ex.Message}";
            _logger.LogError("Failed to export settings", ex);
        }
    }

    [RelayCommand]
    private void ImportSettings()
    {
        try
        {
            var dialog = new OpenFileDialog
            {
                Title = "Import Settings",
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                DefaultExt = ".json"
            };

            if (dialog.ShowDialog() != true)
                return;

            var json = File.ReadAllText(dialog.FileName);
            var exportData = JsonSerializer.Deserialize<ExportData>(json);
            
            if (exportData == null)
            {
                MessageBox.Show("Invalid export file format.", "Import Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Confirm import
            var confirmResult = MessageBox.Show(
                $"This will replace all your current settings:\n\n" +
                $"• {exportData.Rules.Count} routing rules\n" +
                $"• {exportData.FavoriteSites.Count} favorite sites\n\n" +
                $"Exported on: {exportData.ExportDate:yyyy-MM-dd HH:mm}\n\n" +
                "Continue?",
                "Confirm Import",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirmResult != MessageBoxResult.Yes)
                return;

            var result = ProcessImport(exportData);
            
            // Show result
            MessageBox.Show(
                result.GetSummary(),
                result.Success ? "Import Complete" : "Import Failed",
                MessageBoxButton.OK,
                result.Success ? (result.HasWarnings ? MessageBoxImage.Warning : MessageBoxImage.Information) : MessageBoxImage.Error);

            if (result.Success)
            {
                StatusMessage = $"Imported {result.RulesImported} rules, {result.FavoritesImported} favorites";
                HasUnsavedChanges = true;
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Import failed: {ex.Message}";
            _logger.LogError("Failed to import settings", ex);
            MessageBox.Show($"Failed to import settings:\n{ex.Message}", "Import Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private ImportResult ProcessImport(ExportData data)
    {
        var result = new ImportResult { Success = true };

        try
        {
            // Clear existing rules
            Rules.Clear();

            // Import rules
            foreach (var rule in data.Rules)
            {
                // Check if browser exists
                if (!string.IsNullOrEmpty(rule.BrowserPath) && !File.Exists(rule.BrowserPath))
                {
                    rule.Enabled = false;
                    result.DisabledRules.Add($"{rule.Name} ({Path.GetFileName(rule.BrowserPath)})");
                }

                Rules.Add(new RuleViewModel(rule));
                result.RulesImported++;
            }

            // Import favorites
            _config.Config.FavoriteSites.Clear();
            foreach (var site in data.FavoriteSites)
            {
                // Check if custom browser exists
                if (!string.IsNullOrEmpty(site.BrowserPath) && !File.Exists(site.BrowserPath))
                {
                    site.BrowserPath = null; // Reset to auto-routing
                    result.Warnings.Add($"Favorite \"{site.Name}\" reset to auto-routing");
                }

                _config.Config.FavoriteSites.Add(site);
                result.FavoritesImported++;
            }

            // Import settings
            if (data.Settings != null)
            {
                // Default browser - only if exists
                if (!string.IsNullOrEmpty(data.Settings.DefaultBrowserPath))
                {
                    if (File.Exists(data.Settings.DefaultBrowserPath))
                    {
                        DefaultBrowserPath = data.Settings.DefaultBrowserPath;
                    }
                    else
                    {
                        result.Warnings.Add("Default browser path skipped (not found)");
                    }
                }

                if (!string.IsNullOrEmpty(data.Settings.DefaultBrowserArguments))
                {
                    DefaultBrowserArguments = data.Settings.DefaultBrowserArguments;
                }

                DeveloperMode = data.Settings.DeveloperMode;

                if (!string.IsNullOrEmpty(data.Settings.FavoriteSitesHotkey))
                {
                    FavoriteSitesHotkey = data.Settings.FavoriteSitesHotkey;
                }

                if (!string.IsNullOrEmpty(data.Settings.ClipboardUrlHotkey))
                {
                    ClipboardUrlHotkey = data.Settings.ClipboardUrlHotkey;
                }
            }

            // Refresh UI
            UpdateOrders();
            RefreshBrowserGroups();
            UpdateSelectedDefaultBrowser();

            _logger.LogInfo($"Import successful: {result.RulesImported} rules, {result.FavoritesImported} favorites");
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            _logger.LogError("Import processing failed", ex);
        }

        return result;
    }

    partial void OnDefaultBrowserPathChanged(string value)
    {
        HasUnsavedChanges = true;
        UpdateSelectedDefaultBrowser();
    }

    partial void OnSelectedDefaultBrowserChanged(InstalledBrowser? value)
    {
        if (value != null && !value.ExePath.Equals(DefaultBrowserPath, StringComparison.OrdinalIgnoreCase))
        {
            DefaultBrowserPath = value.ExePath;
        }
    }
    partial void OnDefaultBrowserArgumentsChanged(string value) => HasUnsavedChanges = true;
    partial void OnMinimizeToTrayOnCloseChanged(bool value) => HasUnsavedChanges = true;
    partial void OnStartMinimizedChanged(bool value) => HasUnsavedChanges = true;
    partial void OnFocusBrowserAfterRoutingChanged(bool value) => HasUnsavedChanges = true;
    partial void OnStartWithWindowsChanged(bool value) => HasUnsavedChanges = true;
    
    partial void OnDeveloperModeChanged(bool value)
    {
        HasUnsavedChanges = true;
        OnPropertyChanged(nameof(AvailablePatternTypes));
        OnPropertyChanged(nameof(WindowTitle));
    }
}
