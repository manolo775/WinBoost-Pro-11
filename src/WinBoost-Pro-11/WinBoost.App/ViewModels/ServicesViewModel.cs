using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using WinBoost.App.Commands;
using WinBoost.App.Models;
using WinBoost.App.Services.ServicesManager;

namespace WinBoost.App.ViewModels
{
    public class ServicesViewModel : INotifyPropertyChanged
    {
        private readonly WindowsServiceScanner _serviceScanner;

        private bool _isScanning;
        private string _scanStatus = "Neverificat";
        private string _scanMessage =
            "Apasă Scan Services pentru verificare.";

        public ObservableCollection<WindowsServiceInfo>
            Services
        { get; }

        public ICommand ScanServicesCommand { get; }

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

        public string ScanMessage
        {
            get => _scanMessage;

            private set
            {
                if (_scanMessage == value)
                    return;

                _scanMessage = value;
                OnPropertyChanged();
            }
        }

        public string ScanButtonText =>
            IsScanning
                ? "Se scanează..."
                : "Scan Services";

        public ServicesViewModel()
        {
            _serviceScanner =
                new WindowsServiceScanner();

            Services =
                new ObservableCollection<WindowsServiceInfo>();

            ScanServicesCommand =
                new RelayCommand(
                    async _ => await ScanServicesAsync(),
                    _ => !IsScanning);
        }

        private async Task ScanServicesAsync()
        {
            if (IsScanning)
                return;

            IsScanning = true;
            ScanStatus = "Se verifică";
            ScanMessage =
                "Se analizează serviciile Windows...";

            try
            {
                var services =
                    await _serviceScanner.ScanAsync();

                Services.Clear();

                foreach (WindowsServiceInfo service
                         in services)
                {
                    Services.Add(service);
                }

                ScanStatus = "Verificat";

                ScanMessage = services.Count == 0
                    ? "Nu au fost găsite serviciile monitorizate."
                    : $"Scanare finalizată: {services.Count} servicii analizate.";
            }
            catch (Exception ex)
            {
                ScanStatus = "Eroare";

                ScanMessage =
                    $"Scanarea nu a putut fi finalizată: {ex.Message}";
            }
            finally
            {
                IsScanning = false;
            }
        }

        public event PropertyChangedEventHandler?
            PropertyChanged;

        protected void OnPropertyChanged(
            [CallerMemberName]
            string? propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }
    }
}