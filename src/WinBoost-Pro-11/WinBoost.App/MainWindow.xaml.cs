using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using WinBoost.App.Helpers;
using WinBoost.App.Localization;
using WinBoost.App.Services.Licensing;
using WinBoost.App.Services.Navigation;
using WinBoost.App.Views;

using AppLanguage =
    WinBoost.App.Localization.Language;

namespace WinBoost.App
{
    public partial class MainWindow : Window
    {
        private readonly DashboardView
            _dashboardView;

        private readonly LicenseService
            _licenseService;

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

        private SettingsView?
            _settingsView;

        private RecoveryView?
            _recoveryView;

        public MainWindow()
        {
            InitializeComponent();

            _licenseService =
                LicenseService.Instance;

            AppNavigationService.NavigationRequested +=
                AppNavigationService_NavigationRequested;

            _dashboardView =
                new DashboardView();

            NavigateToDashboard();

            RestorePageAfterPrivilegeRestart();

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
                    if (_licenseService.IsActive)
                    {
                        _performanceView ??=
                            new PerformanceView();
                    }
                }),
                DispatcherPriority.ApplicationIdle);
        }

        // ======================================
        // LICENSE ACCESS
        // ======================================

        public bool EnsureLicensedAccess()
        {
            if (_licenseService.IsActive)
            {
                return true;
            }

            NativeMessageDialog.Show(
                this,
                LocalizationHelper.Get(
                    "LicenseRequiredTitle"),
                LocalizationHelper.Get(
                    "LicenseRequiredMessage"),
                LocalizationHelper.Get(
                    "CommonClose"));

            return false;
        }

        // ======================================
        // DASHBOARD
        // ======================================

        private void DashboardButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            NavigateToDashboard();
        }

        public void NavigateToDashboard()
        {
            MainContent.Content =
                _dashboardView;

            AppNavigationService.SetCurrentPage(
                "Dashboard");

            SetActiveButton(
                DashboardButton);
        }

        // ======================================
        // PERFORMANCE
        // ======================================

        private void PerformanceButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            NavigateToPerformance();
        }

        public void NavigateToPerformance()
        {
            if (!EnsureLicensedAccess())
            {
                return;
            }

            _performanceView ??=
                new PerformanceView();

            MainContent.Content =
                _performanceView;

            AppNavigationService.SetCurrentPage(
                "Performance");

            SetActiveButton(
                PerformanceButton);
        }

        // ======================================
        // PRIVACY
        // ======================================

        private void PrivacyButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            NavigateToPrivacy();
        }

        public void NavigateToPrivacy()
        {
            if (!EnsureLicensedAccess())
            {
                return;
            }

            _privacyView ??=
                new PrivacyView();

            MainContent.Content =
                _privacyView;

            AppNavigationService.SetCurrentPage(
                "Privacy");

            SetActiveButton(
                PrivacyButton);
        }

        // ======================================
        // SERVICES
        // ======================================

        private void ServicesButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            NavigateToServices();
        }

        public void NavigateToServices()
        {
            if (!EnsureLicensedAccess())
            {
                return;
            }

            _servicesView ??=
                new ServicesView();

            MainContent.Content =
                _servicesView;

            AppNavigationService.SetCurrentPage(
                "Services");

            SetActiveButton(
                ServicesButton);
        }

        // ======================================
        // WINDOWS UPDATE
        // ======================================

        private void WindowsUpdateButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            NavigateToWindowsUpdate();
        }

        public void NavigateToWindowsUpdate()
        {
            if (!EnsureLicensedAccess())
            {
                return;
            }

            _windowsUpdateView ??=
                new WindowsUpdateView();

            MainContent.Content =
                _windowsUpdateView;

            AppNavigationService.SetCurrentPage(
                "WindowsUpdate");

            SetActiveButton(
                WindowsUpdateButton);
        }

        // ======================================
        // APPS
        // ======================================

        private void AppsButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            NavigateToApps();
        }

        public void NavigateToApps()
        {
            if (!EnsureLicensedAccess())
            {
                return;
            }

            _appsView ??=
                new AppsView();

            MainContent.Content =
                _appsView;

            AppNavigationService.SetCurrentPage(
                "Apps");

            SetActiveButton(
                AppsButton);
        }

        // ======================================
        // STARTUP
        // ======================================

        private void StartupButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            NavigateToStartup();
        }

        public void NavigateToStartup()
        {
            if (!EnsureLicensedAccess())
            {
                return;
            }

            _startupView ??=
                new StartupView();

            MainContent.Content =
                _startupView;

            AppNavigationService.SetCurrentPage(
                "Startup");

            SetActiveButton(
                StartupButton);
        }

        // ======================================
        // RECOVERY
        // ======================================

        private void RecoveryButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            NavigateToRecovery();
        }

        public void NavigateToRecovery()
        {
            if (!EnsureLicensedAccess())
            {
                return;
            }

            _recoveryView ??=
                new RecoveryView();

            MainContent.Content =
                _recoveryView;

            AppNavigationService.SetCurrentPage(
                "Recovery");

            SetActiveButton(
                RecoveryButton);
        }

        // ======================================
        // SETTINGS
        // ======================================

        private void SettingsButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            NavigateToSettings();
        }

        public void NavigateToSettings()
        {
            _settingsView ??=
                new SettingsView();

            MainContent.Content =
                _settingsView;

            AppNavigationService.SetCurrentPage(
                "Settings");

            SetActiveButton(
                SettingsButton);
        }

        // ======================================
        // GLOBAL NAVIGATION
        // ======================================

        private void RestorePageAfterPrivilegeRestart()
        {
            string[] arguments =
                Environment.GetCommandLineArgs();

            for (int index = 0;
                 index < arguments.Length - 1;
                 index++)
            {
                if (!string.Equals(
                        arguments[index],
                        "--return-page",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string returnPage =
                    arguments[index + 1];

                if (string.IsNullOrWhiteSpace(
                        returnPage))
                {
                    return;
                }

                AppNavigationService.NavigateTo(
                    returnPage);

                return;
            }
        }

        private void AppNavigationService_NavigationRequested(
            string page)
        {
            switch (page)
            {
                case "Dashboard":
                    NavigateToDashboard();
                    break;

                case "Performance":
                    NavigateToPerformance();
                    break;

                case "Privacy":
                    NavigateToPrivacy();
                    break;

                case "Services":
                    NavigateToServices();
                    break;

                case "WindowsUpdate":
                    NavigateToWindowsUpdate();
                    break;

                case "Apps":
                    NavigateToApps();
                    break;

                case "Startup":
                    NavigateToStartup();
                    break;

                case "Recovery":
                    NavigateToRecovery();
                    break;

                case "Settings":
                    NavigateToSettings();
                    break;
            }
        }

        // ======================================
        // SIDEBAR ACTIVE BUTTON
        // ======================================

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

            SettingsButton.Style =
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

            RecoveryButton.Style =
                (Style)FindResource(
                    "SidebarButtonStyle");

            activeButton.Style =
                (Style)FindResource(
                    "SidebarActiveButtonStyle");
        }
    }
}