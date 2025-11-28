using System.Windows;
using System.Windows.Input;
using SemaphURL.Models;
using SemaphURL.Services;
using SemaphURL.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace SemaphURL.Views;

/// <summary>
/// Interaction logic for UrlHistoryWindow.xaml
/// </summary>
public partial class UrlHistoryWindow : Wpf.Ui.Controls.FluentWindow
{
    public UrlHistoryWindow()
    {
        InitializeComponent();
    }

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);

        // Subscribe to rule creation events
        if (DataContext is UrlHistoryViewModel vm)
        {
            vm.OnRuleCreationRequested += OnRuleCreationRequested;
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        // Unsubscribe from events
        if (DataContext is UrlHistoryViewModel vm)
        {
            vm.OnRuleCreationRequested -= OnRuleCreationRequested;
        }

        base.OnClosed(e);
    }

    private void OnRuleCreationRequested(RuleCreationRequest request)
    {
        try
        {
            // Get the configuration service and add the rule
            var config = App.Instance.Services.GetRequiredService<IConfigurationService>();
            
            var newRule = new RoutingRule
            {
                Name = request.Name,
                Pattern = request.Pattern,
                PatternType = request.PatternType,
                BrowserPath = request.BrowserPath,
                BrowserArgumentsTemplate = "\"{url}\"",
                Enabled = true,
                Order = config.Config.Rules.Count
            };

            config.Config.Rules.Add(newRule);
            _ = config.SaveAsync();

            MessageBox.Show(
                $"Rule '{request.Name}' created successfully!\n\nPattern: {request.Pattern}\nType: {request.PatternType}",
                "Rule Created",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to create rule: {ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}

