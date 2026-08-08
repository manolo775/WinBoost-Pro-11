using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using WinBoost.App.Localization;
using WinBoost.App.Views;
using AppLanguage =
    WinBoost.App.Localization.Language;

namespace WinBoost.App
{
    public partial class MainWindow : Window
    {
        private readonly DashboardView
            _dashboardView;

        private PerformanceView?
            _performanceView;

        private PrivacyView?
            _privacyView;

        private ServicesView?
            _servicesView;

        private WindowsUpdateView?
            _windowsUpdateView;

        private AppsView?
            _appsView;

        private StartupView?
            _startupView;

        public MainWindow()
        {
            InitializeComponent();

            _dashboardView =
                new DashboardView();

            MainContent.Content =
                _dashboardView;

            SetActiveButton(
                DashboardButton);

            ContentRendered +=
                MainWindow_ContentRendered;
        }

        private void
            LanguageComboBox_SelectionChanged(
                object sender,
                SelectionChangedEventArgs e)
        {
            if (sender is not ComboBox comboBox)
            {
                return;
            }

            if (comboBox.SelectedIndex < 0)
            {
                return;
            }

            AppLanguage selectedLanguage =
                comboBox.SelectedIndex == 1
                    ? AppLanguage.English
                    : AppLanguage.Romanian;

            LanguageManager.Instance.SetLanguage(
                selectedLanguage);
        }

        private void MainWindow_ContentRendered(
            object? sender,
            EventArgs e)
        {
            ContentRendered -=
                MainWindow_ContentRendered;

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
            MainContent.Content =
                _dashboardView;

            SetActiveButton(
                DashboardButton);
        }

        private void PerformanceButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            NavigateToPerformance();
        }

        public void NavigateToPerformance()
        {
            _performanceView ??=
                new PerformanceView();

            MainContent.Content =
                _performanceView;

            SetActiveButton(
                PerformanceButton);
        }

        private void PrivacyButton_Click(
     object sender,
     RoutedEventArgs e)
        {
            NavigateToPrivacy();
        }

        public void NavigateToPrivacy()
        {
            _privacyView ??=
                new PrivacyView();

            MainContent.Content =
                _privacyView;

            SetActiveButton(
                PrivacyButton);
        }

        private void ServicesButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            NavigateToServices();
        }

        private void WindowsUpdateButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            NavigateToWindowsUpdate();
        }

        public void NavigateToWindowsUpdate()
        {
            _windowsUpdateView ??=
                new WindowsUpdateView();

            MainContent.Content =
                _windowsUpdateView;

            SetActiveButton(
                WindowsUpdateButton);
        }

        private void AppsButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            _appsView ??=
                new AppsView();

            MainContent.Content =
                _appsView;

            SetActiveButton(
                AppsButton);
        }

        private void StartupButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            NavigateToStartup();
        }

        public void NavigateToStartup()
        {
            _startupView ??=
                new StartupView();

            MainContent.Content =
                _startupView;

            SetActiveButton(
                StartupButton);
        }

        public void NavigateToServices()
        {
            _servicesView ??=
                new ServicesView();

            MainContent.Content =
                _servicesView;

            SetActiveButton(
                ServicesButton);
        }

        private void SetActiveButton(
            Button activeButton)
        {
            DashboardButton.Style =
                (Style)FindResource(
                    "SidebarButtonStyle");

            PerformanceButton.Style =
                (Style)FindResource(
                    "SidebarButtonStyle");

            PrivacyButton.Style =
                (Style)FindResource(
                    "SidebarButtonStyle");

            ServicesButton.Style =
                (Style)FindResource(
                    "SidebarButtonStyle");

            WindowsUpdateButton.Style =
                (Style)FindResource(
                    "SidebarButtonStyle");

            AppsButton.Style =
                (Style)FindResource(
                    "SidebarButtonStyle");

            StartupButton.Style =
                (Style)FindResource(
                    "SidebarButtonStyle");

            activeButton.Style =
                (Style)FindResource(
                    "SidebarActiveButtonStyle");
        }
    }
}