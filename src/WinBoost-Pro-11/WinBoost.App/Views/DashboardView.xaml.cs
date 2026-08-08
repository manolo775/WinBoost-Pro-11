using System.Windows;
using System.Windows.Controls;
using WinBoost.App.ViewModels;

namespace WinBoost.App.Views
{
    public partial class DashboardView : UserControl
    {
        private readonly DashboardViewModel
            _viewModel;

        public DashboardView()
        {
            InitializeComponent();

            _viewModel =
                new DashboardViewModel();

            DataContext =
                _viewModel;

            Loaded +=
                DashboardView_Loaded;

            Unloaded +=
                DashboardView_Unloaded;
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

        private void PerformanceMetricCard_Click(
             object sender,
              RoutedEventArgs e)
        {
            Window? window =
                Window.GetWindow(this);

            if (window is MainWindow mainWindow)
            {
                mainWindow.NavigateToPerformance();
            }
        }

        private void HealthCard_PerformanceRequested(
    object sender,
    RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is
                MainWindow mainWindow)
            {
                mainWindow.NavigateToPerformance();
            }
        }

        private void HealthCard_ServicesRequested(
            object sender,
            RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is
                MainWindow mainWindow)
            {
                mainWindow.NavigateToServices();
            }
        }

        private void HealthCard_StartupRequested(
            object sender,
            RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is
                MainWindow mainWindow)
            {
                mainWindow.NavigateToStartup();
            }
        }

        private void HealthCard_PrivacyRequested(
            object sender,
            RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is
                MainWindow mainWindow)
            {
                mainWindow.NavigateToPrivacy();
            }
        }

        private void HealthCard_WindowsUpdateRequested(
            object sender,
            RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is
                MainWindow mainWindow)
            {
                mainWindow.NavigateToWindowsUpdate();
            }
        }

        private void PerformanceAlertCard_DetailsRequested(
    object sender,
    RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is
                MainWindow mainWindow)
            {
                mainWindow.NavigateToPerformance();
            }
        }

    }
}