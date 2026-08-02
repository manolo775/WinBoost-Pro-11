using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using WinBoost.App.Commands;
using WinBoost.App.Models;
using WinBoost.App.Services.Health;
using WinBoost.App.Services.Startup;

namespace WinBoost.App.ViewModels
{
    public class StartupViewModel : INotifyPropertyChanged
    {
        private readonly StartupAppsScanner
            _startupAppsScanner;

        private readonly StartupAppsManager
            _startupAppsManager;

        private readonly SystemHealthStateService
            _healthStateService;

        private bool _isScanning;
        private bool _isChangingStartupState;

        private string _scanStatus =
            "Apasă Scan Startup pentru a verifica aplicațiile care pornesc cu Windows.";

        private string _scanBadgeText =
            "Neverificat";

        private Brush _scanBadgeBrush =
            Brushes.LightGray;

        public ObservableCollection<StartupAppInfo>
            StartupApplications
        {
            get;
        }

        public ICommand ScanStartupCommand
        {
            get;
        }

        public ICommand ToggleStartupCommand
        {
            get;
        }

        public string ScanStatus
        {
            get => _scanStatus;

            private set
            {
                if (_scanStatus == value)
                {
                    return;
                }

                _scanStatus = value;
                OnPropertyChanged();
            }
        }

        public string ScanBadgeText
        {
            get => _scanBadgeText;

            private set
            {
                if (_scanBadgeText == value)
                {
                    return;
                }

                _scanBadgeText = value;
                OnPropertyChanged();
            }
        }

        public Brush ScanBadgeBrush
        {
            get => _scanBadgeBrush;

            private set
            {
                if (_scanBadgeBrush == value)
                {
                    return;
                }

                _scanBadgeBrush = value;
                OnPropertyChanged();
            }
        }

        public bool IsScanning
        {
            get => _isScanning;

            private set
            {
                if (_isScanning == value)
                {
                    return;
                }

                _isScanning = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(ScanButtonText));

                CommandManager.InvalidateRequerySuggested();
            }
        }

        public bool IsChangingStartupState
        {
            get => _isChangingStartupState;

            private set
            {
                if (_isChangingStartupState == value)
                {
                    return;
                }

                _isChangingStartupState = value;

                OnPropertyChanged();

                CommandManager.InvalidateRequerySuggested();
            }
        }

        public string ScanButtonText =>
            IsScanning
                ? "Scanning..."
                : "Scan Startup";

        public StartupViewModel()
        {
            _startupAppsScanner =
                new StartupAppsScanner();

            _startupAppsManager =
                new StartupAppsManager();

            _healthStateService =
                SystemHealthStateService.Instance;

            StartupApplications =
                new ObservableCollection<StartupAppInfo>();

            ScanStartupCommand =
                new RelayCommand(
                    async _ =>
                        await ScanStartupAsync(),
                    _ =>
                        !IsScanning &&
                        !IsChangingStartupState);

            ToggleStartupCommand =
                new RelayCommand(
                    async parameter =>
                        await ToggleStartupApplicationAsync(
                            parameter as StartupAppInfo),
                    parameter =>
                        parameter is StartupAppInfo &&
                        !IsScanning &&
                        !IsChangingStartupState);
        }

        private async Task ScanStartupAsync()
        {
            if (IsScanning ||
                IsChangingStartupState)
            {
                return;
            }

            IsScanning = true;

            ScanStatus =
                "Se verifică aplicațiile care pornesc cu Windows...";

            ScanBadgeText =
                "Se verifică";

            ScanBadgeBrush =
                Brushes.Orange;

            try
            {
                var applications =
                    await _startupAppsScanner
                        .ScanAsync();

                StartupApplications.Clear();

                foreach (StartupAppInfo application
                         in applications)
                {
                    StartupApplications.Add(
                        application);
                }

                UpdateStartupHealthScore();

                ScanStatus =
                    StartupApplications.Count == 0
                        ? "Scanare finalizată: nu au fost găsite aplicații configurate pentru pornire automată."
                        : $"Scanare finalizată: " +
                          $"{StartupApplications.Count} aplicații găsite.";

                ScanBadgeText =
                    "Verificat";

                ScanBadgeBrush =
                    Brushes.LimeGreen;
            }
            catch (Exception ex)
            {
                ScanStatus =
                    $"Scanarea nu a putut fi finalizată: " +
                    $"{ex.Message}";

                ScanBadgeText =
                    "Eroare";

                ScanBadgeBrush =
                    Brushes.OrangeRed;
            }
            finally
            {
                IsScanning = false;
            }
        }

        private async Task ToggleStartupApplicationAsync(
            StartupAppInfo? application)
        {
            if (application == null ||
                IsScanning ||
                IsChangingStartupState)
            {
                return;
            }

            bool enableApplication =
                !application.IsEnabled;

            IsChangingStartupState = true;

            ScanBadgeText =
                "Se modifică";

            ScanBadgeBrush =
                Brushes.Orange;

            ScanStatus =
                enableApplication
                    ? $"Se activează aplicația „{application.Name}”..."
                    : $"Se dezactivează aplicația „{application.Name}”...";

            try
            {
                await _startupAppsManager
                    .SetEnabledAsync(
                        application,
                        enableApplication);

                application.IsEnabled =
                    enableApplication;

                UpdateStartupHealthScore();

                ScanStatus =
                    enableApplication
                        ? $"Aplicația „{application.Name}” a fost activată."
                        : $"Aplicația „{application.Name}” a fost dezactivată.";

                ScanBadgeText =
                    "Modificat";

                ScanBadgeBrush =
                    Brushes.LimeGreen;
            }
            catch (Exception ex)
            {
                ScanStatus =
                    $"Operația nu a putut fi finalizată: " +
                    $"{ex.Message}";

                ScanBadgeText =
                    "Eroare";

                ScanBadgeBrush =
                    Brushes.OrangeRed;
            }
            finally
            {
                IsChangingStartupState = false;
            }
        }

        private void UpdateStartupHealthScore()
        {
            int totalStartupApps =
                StartupApplications.Count;

            int enabledStartupApps =
                StartupApplications.Count(
                    application =>
                        application.IsEnabled);

            _healthStateService.UpdateStartupData(
                totalStartupApps,
                enabledStartupApps);
        }

        public event PropertyChangedEventHandler?
            PropertyChanged;

        protected void OnPropertyChanged(
            [CallerMemberName]
            string? propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(
                    propertyName));
        }
    }
}