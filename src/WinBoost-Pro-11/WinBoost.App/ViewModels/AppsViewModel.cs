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
    public class AppsViewModel : INotifyPropertyChanged
    {
        private readonly InstalledAppsScanner _installedAppsScanner;

        private bool _isScanning;

        private string _scanStatus =
            "Apasă Scan Apps pentru a căuta aplicațiile instalate.";

        private string _scanBadgeText = "Neverificat";

        private Brush _scanBadgeBrush = Brushes.LightGray;

        public ObservableCollection<InstalledAppInfo> Applications { get; }

        public ICommand ScanAppsCommand { get; }

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
                : "Scan Apps";

        public AppsViewModel()
        {
            _installedAppsScanner = new InstalledAppsScanner();

            Applications =
                new ObservableCollection<InstalledAppInfo>();

            ScanAppsCommand =
                new RelayCommand(
                    async _ => await ScanAppsAsync(),
                    _ => !IsScanning);
        }

        private async Task ScanAppsAsync()
        {
            if (IsScanning)
                return;

            IsScanning = true;
            ScanStatus = "Se caută aplicațiile instalate...";
            ScanBadgeText = "Se verifică";
            ScanBadgeBrush = Brushes.Orange;

            try
            {
                var applications =
                    await _installedAppsScanner.ScanAsync();

                Applications.Clear();

                foreach (InstalledAppInfo application in applications)
                {
                    Applications.Add(application);
                }

                ScanStatus =
                    $"Scanare finalizată: {Applications.Count} aplicații găsite.";

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