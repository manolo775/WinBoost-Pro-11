using System.Windows.Controls;
using WinBoost.App.ViewModels;
using System.Windows;
using System.Windows.Controls;
using WinBoost.App.ViewModels;

namespace WinBoost.App.Views
{
    public partial class StartupView : UserControl
    {
        public StartupView()
        {
            InitializeComponent();

            DataContext = new StartupViewModel();
        }

        private async void StartupView_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            if (DataContext is StartupViewModel viewModel)
            {
                await viewModel
                    .EnsureStartupApplicationsLoadedAsync();
            }
        }
    }
}