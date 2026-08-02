using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using WinBoost.App.Commands;
using WinBoost.App.Models;
using WinBoost.App.Services.ServicesManager;

namespace WinBoost.App.ViewModels
{
    public class ServicesViewModel : INotifyPropertyChanged
    {
        private readonly WindowsServiceManager
            _windowsServiceManager;

        private readonly WindowsServiceController
            _windowsServiceController;

        private readonly ServiceStartupTypeViewModel
            _startupTypeViewModel;

        private readonly List<WindowsServiceInfo>
            _allServices;

        private readonly DispatcherTimer
            _searchDelayTimer;

        private bool _isScanning;

        private string _scanStatus =
            "Neverificat";

        private string _scanMessage =
            "Apasă Scan Services pentru verificare.";

        private string _searchText =
            string.Empty;

        private string _selectedFilter =
            "Toate";

        public ObservableCollection<WindowsServiceInfo>
            Services
        {
            get;
        }

        public ObservableCollection<string>
            AvailableFilters
        {
            get;
        }

        public ICommand ScanServicesCommand
        {
            get;
        }

        public ICommand StartServiceCommand
        {
            get;
        }

        public ICommand StopServiceCommand
        {
            get;
        }

        public ICommand RestartServiceCommand
        {
            get;
        }

        public ICommand ChangeStartupTypeCommand
        {
            get;
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

        public string ScanMessage
        {
            get => _scanMessage;

            private set
            {
                if (_scanMessage == value)
                {
                    return;
                }

                _scanMessage = value;
                OnPropertyChanged();
            }
        }

        public string SearchText
        {
            get => _searchText;

            set
            {
                if (_searchText == value)
                {
                    return;
                }

                _searchText = value;
                OnPropertyChanged();

                RestartSearchDelay();
            }
        }

        public string SelectedFilter
        {
            get => _selectedFilter;

            set
            {
                if (_selectedFilter == value)
                {
                    return;
                }

                _selectedFilter = value;
                OnPropertyChanged();

                ApplyFilter();
            }
        }

        public string ScanButtonText =>
            IsScanning
                ? "Se scanează..."
                : "Scan Services";

        public ServicesViewModel()
        {
            _windowsServiceManager =
                new WindowsServiceManager();

            _windowsServiceController =
                new WindowsServiceController();

            _startupTypeViewModel =
                new ServiceStartupTypeViewModel();

            _allServices =
                new List<WindowsServiceInfo>();

            Services =
                new ObservableCollection<WindowsServiceInfo>();

            AvailableFilters =
                new ObservableCollection<string>
                {
                    "Toate",
                    "Active",
                    "Oprite",
                    "Automatic",
                    "Automatic (Delayed)",
                    "Manual",
                    "Disabled"
                };

            _searchDelayTimer =
                new DispatcherTimer
                {
                    Interval =
                        TimeSpan.FromMilliseconds(300)
                };

            _searchDelayTimer.Tick +=
                SearchDelayTimer_Tick;

            ScanServicesCommand =
                new RelayCommand(
                    async _ =>
                        await ScanServicesAsync(),
                    _ =>
                        !IsScanning);

            StartServiceCommand =
                new RelayCommand(
                    async parameter =>
                        await StartServiceAsync(
                            parameter as WindowsServiceInfo),
                    parameter =>
                        parameter is WindowsServiceInfo service &&
                        service.CanStart &&
                        !IsScanning);

            StopServiceCommand =
                new RelayCommand(
                    async parameter =>
                        await StopServiceAsync(
                            parameter as WindowsServiceInfo),
                    parameter =>
                        parameter is WindowsServiceInfo service &&
                        service.CanStop &&
                        !IsScanning);

            RestartServiceCommand =
                new RelayCommand(
                    async parameter =>
                        await RestartServiceAsync(
                            parameter as WindowsServiceInfo),
                    parameter =>
                        parameter is WindowsServiceInfo service &&
                        service.CanRestart &&
                        !IsScanning);

            ChangeStartupTypeCommand =
                new RelayCommand(
                    async parameter =>
                    {
                        if (parameter is not
                            WindowsServiceInfo service)
                        {
                            return;
                        }

                        bool wasApplied =
                            await _startupTypeViewModel
                                .ApplyStartupTypeAsync(service);

                        if (wasApplied)
                        {
                            ScanMessage =
                                $"{service.DisplayName}: " +
                                $"tipul de pornire este acum " +
                                $"{service.StartType}.";

                            ApplyFilter();
                        }

                        CommandManager
                            .InvalidateRequerySuggested();
                    },
                    parameter =>
                        parameter is WindowsServiceInfo service &&
                        service.CanChangeStartupType &&
                        service.HasStartupTypeChanged &&
                        !IsScanning);
        }

        private void RestartSearchDelay()
        {
            _searchDelayTimer.Stop();
            _searchDelayTimer.Start();
        }

        private void SearchDelayTimer_Tick(
            object? sender,
            EventArgs e)
        {
            _searchDelayTimer.Stop();

            ApplyFilter();
        }

        private async Task ScanServicesAsync()
        {
            if (IsScanning)
            {
                return;
            }

            IsScanning = true;

            ScanStatus =
                "Se verifică";

            ScanMessage =
                "Se analizează serviciile Windows...";

            try
            {
                var services =
                    await _windowsServiceManager
                        .GetServicesAsync();

                _allServices.Clear();
                _allServices.AddRange(services);

                ApplyFilter();

                ScanStatus =
                    "Verificat";

                ScanMessage =
                    services.Count == 0
                        ? "Nu au fost găsite servicii Windows."
                        : $"Scanare finalizată: " +
                          $"{services.Count} servicii analizate.";
            }
            catch (Exception ex)
            {
                ScanStatus =
                    "Eroare";

                ScanMessage =
                    "Scanarea nu a putut fi finalizată: " +
                    ex.Message;
            }
            finally
            {
                IsScanning = false;
            }
        }

        private async Task StartServiceAsync(
            WindowsServiceInfo? service)
        {
            if (service == null ||
                service.IsBusy)
            {
                return;
            }

            MessageBoxResult confirmation =
                MessageBox.Show(
                    $"Dorești să pornești serviciul:\n\n" +
                    $"{service.DisplayName}\n" +
                    $"({service.ServiceName})?",
                    "Pornire serviciu",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

            if (confirmation != MessageBoxResult.Yes)
            {
                return;
            }

            service.IsBusy = true;

            try
            {
                ServiceOperationResult result =
                    await _windowsServiceController
                        .StartServiceAsync(
                            service.ServiceName);

                if (result.IsSuccessful)
                {
                    UpdateServiceStatus(
                        service,
                        string.IsNullOrWhiteSpace(
                            result.CurrentStatus)
                            ? "Running"
                            : result.CurrentStatus);

                    ScanMessage =
                        $"{service.DisplayName}: " +
                        result.Message;
                }
                else
                {
                    ShowOperationError(
                        result.Message);
                }
            }
            finally
            {
                service.IsBusy = false;

                CommandManager
                    .InvalidateRequerySuggested();
            }
        }

        private async Task StopServiceAsync(
            WindowsServiceInfo? service)
        {
            if (service == null ||
                service.IsBusy)
            {
                return;
            }

            MessageBoxResult confirmation =
                MessageBox.Show(
                    $"Dorești să oprești serviciul:\n\n" +
                    $"{service.DisplayName}\n" +
                    $"({service.ServiceName})?\n\n" +
                    "Oprirea unui serviciu poate afecta " +
                    "funcționarea Windows sau a unor aplicații.",
                    "Oprire serviciu",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

            if (confirmation != MessageBoxResult.Yes)
            {
                return;
            }

            service.IsBusy = true;

            try
            {
                ServiceOperationResult result =
                    await _windowsServiceController
                        .StopServiceAsync(
                            service.ServiceName);

                if (result.IsSuccessful)
                {
                    UpdateServiceStatus(
                        service,
                        string.IsNullOrWhiteSpace(
                            result.CurrentStatus)
                            ? "Stopped"
                            : result.CurrentStatus);

                    ScanMessage =
                        $"{service.DisplayName}: " +
                        result.Message;
                }
                else
                {
                    ShowOperationError(
                        result.Message);
                }
            }
            finally
            {
                service.IsBusy = false;

                CommandManager
                    .InvalidateRequerySuggested();
            }
        }

        private async Task RestartServiceAsync(
            WindowsServiceInfo? service)
        {
            if (service == null ||
                service.IsBusy)
            {
                return;
            }

            MessageBoxResult confirmation =
                MessageBox.Show(
                    $"Dorești să repornești serviciul:\n\n" +
                    $"{service.DisplayName}\n" +
                    $"({service.ServiceName})?",
                    "Repornire serviciu",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

            if (confirmation != MessageBoxResult.Yes)
            {
                return;
            }

            service.IsBusy = true;

            try
            {
                ServiceOperationResult result =
                    await _windowsServiceController
                        .RestartServiceAsync(
                            service.ServiceName);

                if (result.IsSuccessful)
                {
                    UpdateServiceStatus(
                        service,
                        string.IsNullOrWhiteSpace(
                            result.CurrentStatus)
                            ? "Running"
                            : result.CurrentStatus);

                    ScanMessage =
                        $"{service.DisplayName}: " +
                        result.Message;
                }
                else
                {
                    ShowOperationError(
                        result.Message);
                }
            }
            finally
            {
                service.IsBusy = false;

                CommandManager
                    .InvalidateRequerySuggested();
            }
        }

        private void UpdateServiceStatus(
            WindowsServiceInfo service,
            string status)
        {
            service.Status = status;

            service.StatusBrush =
                status.Equals(
                    "Running",
                    StringComparison.OrdinalIgnoreCase)
                    ? Brushes.LimeGreen
                    : Brushes.Orange;

            CommandManager
                .InvalidateRequerySuggested();

            ApplyFilter();
        }

        private static void ShowOperationError(
            string message)
        {
            MessageBox.Show(
                message,
                "Operația nu a putut fi executată",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        private void ApplyFilter()
        {
            IEnumerable<WindowsServiceInfo> filteredServices =
                _allServices;

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string searchValue =
                    SearchText.Trim();

                filteredServices =
                    filteredServices.Where(
                        service =>
                            service.DisplayName.Contains(
                                searchValue,
                                StringComparison.OrdinalIgnoreCase) ||
                            service.ServiceName.Contains(
                                searchValue,
                                StringComparison.OrdinalIgnoreCase));
            }

            filteredServices =
                SelectedFilter switch
                {
                    "Active" =>
                        filteredServices.Where(
                            service =>
                                service.Status.Equals(
                                    "Running",
                                    StringComparison.OrdinalIgnoreCase)),

                    "Oprite" =>
                        filteredServices.Where(
                            service =>
                                !service.Status.Equals(
                                    "Running",
                                    StringComparison.OrdinalIgnoreCase)),

                    "Automatic" =>
                        filteredServices.Where(
                            service =>
                                service.StartType.Equals(
                                    "Automatic",
                                    StringComparison.OrdinalIgnoreCase)),

                    "Automatic (Delayed)" =>
                        filteredServices.Where(
                            service =>
                                service.StartType.Equals(
                                    "Automatic (Delayed)",
                                    StringComparison.OrdinalIgnoreCase)),

                    "Manual" =>
                        filteredServices.Where(
                            service =>
                                service.StartType.Equals(
                                    "Manual",
                                    StringComparison.OrdinalIgnoreCase)),

                    "Disabled" =>
                        filteredServices.Where(
                            service =>
                                service.StartType.Equals(
                                    "Disabled",
                                    StringComparison.OrdinalIgnoreCase)),

                    _ =>
                        filteredServices
                };

            WindowsServiceInfo[] filteredArray =
                filteredServices.ToArray();

            Services.Clear();

            foreach (WindowsServiceInfo service
                     in filteredArray)
            {
                Services.Add(service);
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