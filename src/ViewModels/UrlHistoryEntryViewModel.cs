using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SemaphURL.Models;

namespace SemaphURL.ViewModels;

/// <summary>
/// ViewModel for a single URL history entry
/// </summary>
public partial class UrlHistoryEntryViewModel : ObservableObject
{
    private readonly UrlHistoryEntry _entry;
    private readonly Action<UrlHistoryEntryViewModel>? _onCreateRule;
    private readonly Action<UrlHistoryEntryViewModel>? _onCopyUrl;
    private readonly Action<UrlHistoryEntryViewModel>? _onOpenUrl;
    private readonly Action<UrlHistoryEntryViewModel>? _onDelete;

    public Guid Id => _entry.Id;
    public string Url => _entry.Url;
    public string Domain => _entry.Domain;
    public DateTime Timestamp => _entry.Timestamp;
    public string BrowserPath => _entry.BrowserPath;
    public string BrowserName => _entry.BrowserName;
    public string? RuleName => _entry.RuleName;

    // Formatted properties for display
    public string TimestampFormatted => _entry.Timestamp.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
    public string DateFormatted => _entry.Timestamp.ToLocalTime().ToString("yyyy-MM-dd");
    public string TimeFormatted => _entry.Timestamp.ToLocalTime().ToString("HH:mm:ss");
    public string RuleNameDisplay => _entry.RuleName ?? "Default";
    public string UrlTruncated => _entry.Url.Length > 60 ? _entry.Url[..57] + "..." : _entry.Url;

    [ObservableProperty]
    private bool _isSelected;

    public UrlHistoryEntryViewModel(
        UrlHistoryEntry entry,
        Action<UrlHistoryEntryViewModel>? onCreateRule = null,
        Action<UrlHistoryEntryViewModel>? onCopyUrl = null,
        Action<UrlHistoryEntryViewModel>? onOpenUrl = null,
        Action<UrlHistoryEntryViewModel>? onDelete = null)
    {
        _entry = entry;
        _onCreateRule = onCreateRule;
        _onCopyUrl = onCopyUrl;
        _onOpenUrl = onOpenUrl;
        _onDelete = onDelete;
    }

    [RelayCommand]
    private void CreateRuleFromDomain()
    {
        _onCreateRule?.Invoke(this);
    }

    [RelayCommand]
    private void CopyUrl()
    {
        _onCopyUrl?.Invoke(this);
    }

    [RelayCommand]
    private void OpenUrl()
    {
        _onOpenUrl?.Invoke(this);
    }

    [RelayCommand]
    private void Delete()
    {
        _onDelete?.Invoke(this);
    }

    public UrlHistoryEntry ToModel() => _entry;
}

