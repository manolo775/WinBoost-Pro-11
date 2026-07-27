using System.Windows;
using System.Windows.Controls;
using WinBoost.App.ViewModels;

namespace WinBoost.App.Views
{
    public partial class DashboardView : UserControl
    {
        private readonly DashboardViewModel _viewModel;

        public DashboardView()
        {
            InitializeComponent();

            _viewModel = new DashboardViewModel();
            DataContext = _viewModel;

            Loaded += DashboardView_Loaded;
            Unloaded += DashboardView_Unloaded;
        }

        private void DashboardView_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            _viewModel.StartMonitoring();
        }

        private void DashboardView_Unloaded(
            object sender,
            RoutedEventArgs e)
        {
            _viewModel.StopMonitoring();
        }
    }
}