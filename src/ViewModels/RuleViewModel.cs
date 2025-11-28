using CommunityToolkit.Mvvm.ComponentModel;
using SemaphURL.Models;

namespace SemaphURL.ViewModels;

/// <summary>
/// ViewModel for a single routing rule in the UI
/// </summary>
public partial class RuleViewModel : ObservableObject
{
    private readonly RoutingRule _rule;

    public Guid Id => _rule.Id;

    [ObservableProperty]
    private bool _enabled;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private PatternType _patternType;

    [ObservableProperty]
    private string _pattern = string.Empty;

    [ObservableProperty]
    private string _browserPath = string.Empty;

    [ObservableProperty]
    private string _browserArgumentsTemplate = "\"{url}\"";

    [ObservableProperty]
    private int _order;

    public RuleViewModel() : this(new RoutingRule()) { }

    public RuleViewModel(RoutingRule rule)
    {
        _rule = rule;
        LoadFromRule();
    }

    private void LoadFromRule()
    {
        Enabled = _rule.Enabled;
        Name = _rule.Name;
        PatternType = _rule.PatternType;
        Pattern = _rule.Pattern;
        BrowserPath = _rule.BrowserPath;
        BrowserArgumentsTemplate = _rule.BrowserArgumentsTemplate;
        Order = _rule.Order;
    }

    public RoutingRule ToRule() => new()
    {
        Id = _rule.Id,
        Enabled = Enabled,
        Name = Name,
        PatternType = PatternType,
        Pattern = Pattern,
        BrowserPath = BrowserPath,
        BrowserArgumentsTemplate = BrowserArgumentsTemplate,
        Order = Order
    };

    public static IEnumerable<PatternType> PatternTypes => Enum.GetValues<PatternType>();
}

