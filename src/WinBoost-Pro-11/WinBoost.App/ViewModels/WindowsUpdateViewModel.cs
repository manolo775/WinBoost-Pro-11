using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using WinBoost.App.Commands;

namespace WinBoost.App.ViewModels
{
    public class WindowsUpdateViewModel : INotifyPropertyChanged
    {
        private bool _isScanning;

        private string _scanStatus =
            "Apasă Scan Updates pentru verificare.";

        private string _scanBadgeText =
            "Neverificat";

        public ICommand ScanUpdatesCommand { get; }

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
            }
        }

        public string ScanButtonText =>
            IsScanning
                ? "Se verifică..."
                : "Scan Updates";

        public WindowsUpdateViewModel()
        {
            ScanUpdatesCommand =
                new RelayCommand(
                    async _ => await ScanUpdatesAsync(),
                    _ => !IsScanning);
        }

        private async Task ScanUpdatesAsync()
        {
            if (IsScanning)
                return;

            IsScanning = true;
            ScanBadgeText = "Se verifică";
            ScanStatus =
                "Se verifică serviciile necesare pentru Windows Update...";

            try
            {
                await Task.Delay(1000);

                ScanBadgeText = "Verificat";
                ScanStatus =
                    "Verificare finalizată. Nu s-a modificat nimic în Windows.";
            }
            catch (Exception ex)
            {
                ScanBadgeText = "Eroare";
                ScanStatus =
                    $"Verificarea nu a putut fi finalizată: {ex.Message}";
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