using System.Windows;
using WinBoost.App.Views;

namespace WinBoost.App
{
    public partial class MainWindow : Window
    {
        private readonly DashboardView _dashboardView;

        private PerformanceView? _performanceView;

        private PrivacyView? _privacyView;

        public MainWindow()
        {
            InitializeComponent();

            _dashboardView = new DashboardView();

            MainContent.Content = _dashboardView;

            SetActiveButton(DashboardButton);
        }

        private void DashboardButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            MainContent.Content = _dashboardView;

            SetActiveButton(DashboardButton);
        }

        private void PerformanceButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            _performanceView ??=
                new PerformanceView();

            MainContent.Content =
                _performanceView;

            SetActiveButton(PerformanceButton);
        }

        private void PrivacyButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            _privacyView ??=
                new PrivacyView();

            MainContent.Content =
                _privacyView;

            SetActiveButton(PrivacyButton);
        }

        private void SetActiveButton(
            System.Windows.Controls.Button activeButton)
        {
            DashboardButton.Style =
                (Style)FindResource("SidebarButtonStyle");

            PerformanceButton.Style =
                (Style)FindResource("SidebarButtonStyle");

            PrivacyButton.Style =
                (Style)FindResource("SidebarButtonStyle");

            activeButton.Style =
                (Style)FindResource("SidebarActiveButtonStyle");
        }
    }
}