using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using WinBoost.App.Commands;
using WinBoost.App.Models;
using WinBoost.App.Services;

namespace WinBoost.App.ViewModels
{
    public sealed class PrivacyViewModel : INotifyPropertyChanged
    {
        private readonly PrivacyScanService _privacyScanService;

        private bool _isScanning;
        private string _scanStatus =
            "Pornește o scanare pentru a verifica setările de confidențialitate.";

        private string _overallStatus = "Neverificat";

        public ObservableCollection<PrivacyCheckItem> PrivacyItems { get; }

        public ICommand ScanPrivacyCommand { get; }

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

        public string OverallStatus
        {
            get => _overallStatus;

            private set
            {
                if (_overallStatus == value)
                    return;

                _overallStatus = value;
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
                : "Scan Privacy";

        public PrivacyViewModel()
        {
            _privacyScanService = new PrivacyScanService();

            PrivacyItems = new ObservableCollection<PrivacyCheckItem>
            {
                new PrivacyCheckItem
                {
                    Id = "diagnostics",
                    Title = "Diagnostic și telemetrie",
                    Description =
                        "Verifică nivelul datelor de diagnostic trimise către Microsoft."
                },

                new PrivacyCheckItem
                {
                    Id = "advertising-id",
                    Title = "Advertising ID",
                    Description =
                        "Verifică utilizarea identificatorului pentru reclame personalizate."
                },

                new PrivacyCheckItem
                {
                    Id = "activity-history",
                    Title = "Activity History",
                    Description =
                        "Verifică dacă Windows salvează istoricul activităților."
                },

                new PrivacyCheckItem
                {
                    Id = "location-services",
                    Title = "Location Services",
                    Description =
                        "Verifică accesul aplicațiilor la locația dispozitivului."
                }
            };

            ScanPrivacyCommand = new RelayCommand(
                async _ => await ScanPrivacyAsync(),
                _ => !IsScanning);
        }

        private async Task ScanPrivacyAsync()
        {
            if (IsScanning)
                return;

            IsScanning = true;
            OverallStatus = "Se verifică";
            ScanStatus = "Verific setările de confidențialitate...";

            try
            {
                var results = await Task.Run(
                    () => _privacyScanService.Scan());

                PrivacyItems.Clear();

                foreach (PrivacyCheckItem item in results)
                {
                    PrivacyItems.Add(item);
                }

                OverallStatus = "Verificat";
                ScanStatus =
                    "Scanare finalizată: setările au fost verificate. Nu s-a modificat nimic în Windows.";
            }
            catch (Exception ex)
            {
                OverallStatus = "Eroare";
                ScanStatus =
                    $"Scanarea nu a putut fi finalizată: {ex.Message}";
            }
            finally
            {
                IsScanning = false;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(
            [CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }
    }
}