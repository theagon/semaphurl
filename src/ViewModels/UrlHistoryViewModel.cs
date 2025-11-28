using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SemaphURL.Models;
using SemaphURL.Services;

namespace SemaphURL.ViewModels;

/// <summary>
/// ViewModel for the URL History window
/// </summary>
public partial class UrlHistoryViewModel : ObservableObject
{
    private readonly IUrlHistoryService _history;
    private readonly IRoutingService _routing;
    private readonly ILoggingService _logger;
    private readonly IBrowserDiscoveryService _browserDiscovery;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private UrlHistoryEntryViewModel? _selectedEntry;

    [ObservableProperty]
    private bool _isCreatingRule;

    [ObservableProperty]
    private string _newRuleName = string.Empty;

    [ObservableProperty]
    private string _newRulePattern = string.Empty;

    [ObservableProperty]
    private PatternType _newRulePatternType = PatternType.DomainContains;

    [ObservableProperty]
    private InstalledBrowser? _selectedBrowser;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public ObservableCollection<UrlHistoryEntryViewModel> Entries { get; } = [];
    public ObservableCollection<UrlHistoryEntryViewModel> FilteredEntries { get; } = [];

    public IReadOnlyList<InstalledBrowser> InstalledBrowsers => _browserDiscovery.GetInstalledBrowsers();
    public static IEnumerable<PatternType> PatternTypes => Enum.GetValues<PatternType>();

    public int TotalCount => Entries.Count;
    public int FilteredCount => FilteredEntries.Count;

    public UrlHistoryViewModel(
        IUrlHistoryService history,
        IRoutingService routing,
        ILoggingService logger,
        IBrowserDiscoveryService browserDiscovery)
    {
        _history = history;
        _routing = routing;
        _logger = logger;
        _browserDiscovery = browserDiscovery;

        LoadEntries();
    }

    public void LoadEntries()
    {
        Entries.Clear();
        FilteredEntries.Clear();

        foreach (var entry in _history.Entries.OrderByDescending(e => e.Timestamp))
        {
            var vm = CreateEntryViewModel(entry);
            Entries.Add(vm);
            FilteredEntries.Add(vm);
        }

        UpdateStatus();
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(FilteredCount));
    }

    private UrlHistoryEntryViewModel CreateEntryViewModel(UrlHistoryEntry entry)
    {
        return new UrlHistoryEntryViewModel(
            entry,
            onCreateRule: StartCreateRule,
            onCopyUrl: CopyUrlToClipboard,
            onOpenUrl: OpenUrlInBrowser,
            onDelete: DeleteEntry);
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        FilteredEntries.Clear();

        var filtered = string.IsNullOrWhiteSpace(SearchText)
            ? Entries
            : Entries.Where(e =>
                e.Url.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                e.Domain.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                (e.RuleName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                e.BrowserName.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

        foreach (var entry in filtered)
        {
            FilteredEntries.Add(entry);
        }

        OnPropertyChanged(nameof(FilteredCount));
        UpdateStatus();
    }

    private void StartCreateRule(UrlHistoryEntryViewModel entry)
    {
        SelectedEntry = entry;
        NewRuleName = $"Rule for {entry.Domain}";
        NewRulePattern = entry.Domain;
        NewRulePatternType = PatternType.DomainContains;
        
        // Try to find the browser that was used
        SelectedBrowser = InstalledBrowsers.FirstOrDefault(b => 
            b.ExePath.Equals(entry.BrowserPath, StringComparison.OrdinalIgnoreCase)) 
            ?? InstalledBrowsers.FirstOrDefault();
        
        IsCreatingRule = true;
    }

    [RelayCommand]
    private void ConfirmCreateRule()
    {
        if (SelectedBrowser == null || string.IsNullOrWhiteSpace(NewRulePattern))
        {
            StatusMessage = "Please fill in all required fields";
            return;
        }

        // Emit event to create rule - will be handled by the window
        OnRuleCreationRequested?.Invoke(new RuleCreationRequest
        {
            Name = NewRuleName,
            Pattern = NewRulePattern,
            PatternType = NewRulePatternType,
            BrowserPath = SelectedBrowser.ExePath
        });

        CancelCreateRule();
        StatusMessage = "Rule creation requested";
    }

    [RelayCommand]
    private void CancelCreateRule()
    {
        IsCreatingRule = false;
        SelectedEntry = null;
        NewRuleName = string.Empty;
        NewRulePattern = string.Empty;
    }

    private void CopyUrlToClipboard(UrlHistoryEntryViewModel entry)
    {
        try
        {
            Clipboard.SetText(entry.Url);
            StatusMessage = "URL copied to clipboard";
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to copy URL to clipboard", ex);
            StatusMessage = "Failed to copy URL";
        }
    }

    private void OpenUrlInBrowser(UrlHistoryEntryViewModel entry)
    {
        try
        {
            var result = _routing.Route(entry.Url);
            _ = _routing.ExecuteRoutingAsync(result);
            StatusMessage = $"Opened: {entry.Domain}";
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to open URL", ex);
            StatusMessage = "Failed to open URL";
        }
    }

    private async void DeleteEntry(UrlHistoryEntryViewModel entry)
    {
        try
        {
            await _history.DeleteEntryAsync(entry.Id);
            Entries.Remove(entry);
            FilteredEntries.Remove(entry);
            OnPropertyChanged(nameof(TotalCount));
            OnPropertyChanged(nameof(FilteredCount));
            StatusMessage = "Entry deleted";
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to delete history entry", ex);
            StatusMessage = "Failed to delete entry";
        }
    }

    [RelayCommand]
    private void Refresh()
    {
        LoadEntries();
        StatusMessage = "History refreshed";
    }

    [RelayCommand]
    private void DebugRefresh()
    {
        _logger.LogInfo("=== DEBUG REFRESH START ===");
        _logger.LogInfo($"_history.Entries count: {_history.Entries.Count}");
        
        foreach (var e in _history.Entries)
        {
            _logger.LogInfo($"  Entry: {e.Id} | {e.Url} | {e.Timestamp}");
        }

        _logger.LogInfo($"Before clear - Entries.Count: {Entries.Count}, FilteredEntries.Count: {FilteredEntries.Count}");
        
        Entries.Clear();
        FilteredEntries.Clear();
        
        _logger.LogInfo($"After clear - Entries.Count: {Entries.Count}, FilteredEntries.Count: {FilteredEntries.Count}");

        var sorted = _history.Entries.OrderByDescending(e => e.Timestamp).ToList();
        _logger.LogInfo($"Sorted list count: {sorted.Count}");

        foreach (var entry in sorted)
        {
            var vm = CreateEntryViewModel(entry);
            Entries.Add(vm);
            FilteredEntries.Add(vm);
            _logger.LogInfo($"  Added VM: {vm.Domain} | {vm.Url}");
        }

        _logger.LogInfo($"After add - Entries.Count: {Entries.Count}, FilteredEntries.Count: {FilteredEntries.Count}");
        
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(FilteredCount));
        OnPropertyChanged(nameof(FilteredEntries));
        
        UpdateStatus();
        
        _logger.LogInfo($"Final status: {StatusMessage}");
        _logger.LogInfo("=== DEBUG REFRESH END ===");
        
        StatusMessage = $"DEBUG: {FilteredEntries.Count} items loaded. Check log!";
    }

    [RelayCommand]
    private async Task ClearHistory()
    {
        var result = MessageBox.Show(
            "Are you sure you want to clear all URL history?",
            "Clear History",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            await _history.ClearHistoryAsync();
            LoadEntries();
            StatusMessage = "History cleared";
        }
    }

    [RelayCommand]
    private void Close()
    {
        App.Instance.HideUrlHistory();
    }

    private void UpdateStatus()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            StatusMessage = $"{TotalCount} entries";
        }
        else
        {
            StatusMessage = $"{FilteredCount} of {TotalCount} entries";
        }
    }

    // Event for rule creation
    public event Action<RuleCreationRequest>? OnRuleCreationRequested;
}

/// <summary>
/// Request to create a new routing rule
/// </summary>
public class RuleCreationRequest
{
    public string Name { get; set; } = string.Empty;
    public string Pattern { get; set; } = string.Empty;
    public PatternType PatternType { get; set; }
    public string BrowserPath { get; set; } = string.Empty;
}

