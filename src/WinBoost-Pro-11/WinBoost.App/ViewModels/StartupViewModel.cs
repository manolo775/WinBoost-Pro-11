using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using WinBoost.App.Commands;
using WinBoost.App.Models;
using WinBoost.App.Services;

namespace WinBoost.App.ViewModels
{
    public class StartupViewModel : INotifyPropertyChanged
    {
        private readonly StartupAppsScanner _startupAppsScanner;

        private bool _isScanning;

        private string _scanStatus =
            "Apasă Scan Startup pentru a verifica aplicațiile care pornesc cu Windows.";

        private string _scanBadgeText = "Neverificat";

        private Brush _scanBadgeBrush = Brushes.LightGray;

        public ObservableCollection<StartupAppInfo> StartupApplications
        {
            get;
        }

        public ICommand ScanStartupCommand
        {
            get;
        }

        public string ScanStatus
        {
            get => _scanStatus;

            private set
            {
                if (_scanStatus == value)
                    return;

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
                    return;

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
                    return;

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
                    return;

                _isScanning = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(ScanButtonText));

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

            StartupApplications =
                new ObservableCollection<StartupAppInfo>();

            ScanStartupCommand =
                new RelayCommand(
                    async _ => await ScanStartupAsync(),
                    _ => !IsScanning);
        }

        private async Task ScanStartupAsync()
        {
            if (IsScanning)
                return;

            IsScanning = true;
            ScanStatus =
                "Se verifică aplicațiile care pornesc cu Windows...";

            ScanBadgeText = "Se verifică";
            ScanBadgeBrush = Brushes.Orange;

            try
            {
                var applications =
                    await _startupAppsScanner.ScanAsync();

                StartupApplications.Clear();

                foreach (StartupAppInfo application in applications)
                {
                    StartupApplications.Add(application);
                }

                ScanStatus =
                    StartupApplications.Count == 0
                        ? "Scanare finalizată: nu au fost găsite aplicații configurate pentru pornire automată."
                        : $"Scanare finalizată: {StartupApplications.Count} aplicații găsite.";

                ScanBadgeText = "Verificat";
                ScanBadgeBrush = Brushes.LimeGreen;
            }
            catch (Exception ex)
            {
                ScanStatus =
                    $"Scanarea nu a putut fi finalizată: {ex.Message}";

                ScanBadgeText = "Eroare";
                ScanBadgeBrush = Brushes.OrangeRed;
            }
            finally
            {
                IsScanning = false;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(
            [CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }
    }
}