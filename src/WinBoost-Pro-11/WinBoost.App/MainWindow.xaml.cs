using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using WinBoost.App.Views;

namespace WinBoost.App
{
    public partial class MainWindow : Window
    {
        private readonly DashboardView _dashboardView;

        private PerformanceView? _performanceView;
        private PrivacyView? _privacyView;
        private ServicesView? _servicesView;
        private WindowsUpdateView? _windowsUpdateView;
        public MainWindow()
        {
            InitializeComponent();

            _dashboardView = new DashboardView();

            MainContent.Content = _dashboardView;
            SetActiveButton(DashboardButton);

            ContentRendered += MainWindow_ContentRendered;
        }

        private void MainWindow_ContentRendered(
            object? sender,
            EventArgs e)
        {
            ContentRendered -= MainWindow_ContentRendered;

            // Performance este pregătită după afișarea ferestrei,
            // când interfața nu mai este ocupată.
            Dispatcher.BeginInvoke(
                new Action(() =>
                {
                    _performanceView ??=
                        new PerformanceView();
                }),
                DispatcherPriority.ApplicationIdle);
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

            MainContent.Content = _performanceView;
            SetActiveButton(PerformanceButton);
        }

        private void PrivacyButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            _privacyView ??=
                new PrivacyView();

            MainContent.Content = _privacyView;
            SetActiveButton(PrivacyButton);
        }

        private void ServicesButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            _servicesView ??=
                new ServicesView();

            MainContent.Content = _servicesView;
            SetActiveButton(ServicesButton);
        }

        private void SetActiveButton(
            Button activeButton)
        {
            DashboardButton.Style =
                (Style)FindResource("SidebarButtonStyle");

            PerformanceButton.Style =
                (Style)FindResource("SidebarButtonStyle");

            PrivacyButton.Style =
                (Style)FindResource("SidebarButtonStyle");

            ServicesButton.Style =
                (Style)FindResource("SidebarButtonStyle");
            WindowsUpdateButton.Style =
    (Style)FindResource("SidebarButtonStyle");

            activeButton.Style =
                (Style)FindResource("SidebarActiveButtonStyle");
        }
        private void WindowsUpdateButton_Click(
    object sender,
    RoutedEventArgs e)
        {
            _windowsUpdateView ??= new WindowsUpdateView();

            MainContent.Content = _windowsUpdateView;
            SetActiveButton(WindowsUpdateButton);
        }
    }
}