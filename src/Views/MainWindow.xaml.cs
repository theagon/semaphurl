using System.ComponentModel;
using SemaphURL.ViewModels;
using Wpf.Ui.Controls;

namespace SemaphURL.Views;

public partial class MainWindow : FluentWindow
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        // Check for unsaved changes
        if (DataContext is MainViewModel vm && vm.HasUnsavedChanges)
        {
            var result = System.Windows.MessageBox.Show(
                "You have unsaved changes. Do you want to save before closing?",
                "Unsaved Changes",
                System.Windows.MessageBoxButton.YesNoCancel,
                System.Windows.MessageBoxImage.Question);

            switch (result)
            {
                case System.Windows.MessageBoxResult.Yes:
                    vm.SaveCommand.Execute(null);
                    break;
                case System.Windows.MessageBoxResult.Cancel:
                    e.Cancel = true;
                    return;
            }
        }

        // Check if we should minimize to tray
        if (DataContext is MainViewModel viewModel && viewModel.MinimizeToTrayOnClose)
        {
            e.Cancel = true;
            App.Instance.MinimizeToTray();
            return;
        }

        base.OnClosing(e);
    }
}

