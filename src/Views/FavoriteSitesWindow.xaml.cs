using System.Windows;
using System.Windows.Input;
using SemaphURL.ViewModels;

namespace SemaphURL.Views;

/// <summary>
/// Interaction logic for FavoriteSitesWindow.xaml
/// </summary>
public partial class FavoriteSitesWindow : Window
{
    public FavoriteSitesWindow()
    {
        InitializeComponent();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            if (DataContext is FavoriteSitesViewModel vm && vm.IsEditing)
            {
                vm.CancelEditCommand.Execute(null);
            }
            else
            {
                Hide();
            }
            e.Handled = true;
        }
    }

    private void Window_Deactivated(object sender, EventArgs e)
    {
        // Hide when clicking outside, but not when editing
        if (DataContext is FavoriteSitesViewModel vm && !vm.IsEditing)
        {
            Hide();
        }
    }

    private void SiteCard_MouseEnter(object sender, MouseEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is FavoriteSiteViewModel vm)
        {
            vm.IsHovered = true;
        }
    }

    private void SiteCard_MouseLeave(object sender, MouseEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is FavoriteSiteViewModel vm)
        {
            vm.IsHovered = false;
        }
    }

    private void SiteCard_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is FavoriteSiteViewModel vm)
        {
            vm.OpenCommand.Execute(null);
        }
    }
}

