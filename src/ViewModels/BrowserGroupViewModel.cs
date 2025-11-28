using System.Collections.ObjectModel;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SemaphURL.ViewModels;

/// <summary>
/// ViewModel for a browser with its associated rules (for grouped display)
/// </summary>
public partial class BrowserGroupViewModel : ObservableObject
{
    [ObservableProperty]
    private string _browserName = string.Empty;

    [ObservableProperty]
    private string _browserPath = string.Empty;

    [ObservableProperty]
    private ImageSource? _browserIcon;

    [ObservableProperty]
    private bool _isExpanded = true;

    public ObservableCollection<RuleViewModel> Rules { get; } = [];

    public int RuleCount => Rules.Count;

    public void RefreshRuleCount() => OnPropertyChanged(nameof(RuleCount));
}

