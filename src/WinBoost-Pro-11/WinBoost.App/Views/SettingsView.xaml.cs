using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WinBoost.App.ViewModels;

namespace WinBoost.App.Views
{
    public partial class SettingsView : UserControl
    {
        private readonly SettingsViewModel _viewModel;

        public SettingsView()
        {
            InitializeComponent();

            _viewModel = new SettingsViewModel();

            DataContext = _viewModel;
        }

        private void AlertsCard_MouseLeftButtonUp(
            object sender,
            MouseButtonEventArgs e)
        {
            if (IsInsideCheckBox(
                    e.OriginalSource as DependencyObject))
            {
                return;
            }

            if (Window.GetWindow(this)
                is MainWindow mainWindow)
            {
                mainWindow.NavigateToDashboard();
            }
        }

        private static bool IsInsideCheckBox(
            DependencyObject? element)
        {
            while (element != null)
            {
                if (element is CheckBox)
                {
                    return true;
                }

                element =
                    System.Windows.Media.VisualTreeHelper
                        .GetParent(element);
            }

            return false;
        }
    }
}