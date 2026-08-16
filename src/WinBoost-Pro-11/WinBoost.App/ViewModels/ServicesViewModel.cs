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
using WinBoost.App.Helpers;
using WinBoost.App.Localization;
using WinBoost.App.Models;
using WinBoost.App.Services.Health;
using WinBoost.App.Services.ServicesManager;

namespace WinBoost.App.ViewModels
{
    public class ServicesViewModel : INotifyPropertyChanged
    {
        private const string FilterAll = "All";
        private const string FilterRunning = "Running";
        private const string FilterStopped = "Stopped";
        private const string FilterAutomatic = "Automatic";
        private const string FilterAutomaticDelayed =
            "AutomaticDelayed";
        private const string FilterManual = "Manual";
        private const string FilterDisabled = "Disabled";

        private readonly WindowsServiceManager
            _windowsServiceManager;

        private readonly WindowsServiceController
            _windowsServiceController;

        private readonly SystemHealthStateService
            _healthStateService;

        private readonly ServiceStartupTypeViewModel
            _startupTypeViewModel;

        private readonly List<WindowsServiceInfo>
            _allServices;

        private readonly DispatcherTimer
            _searchDelayTimer;

        private bool _isScanning;
        private string _scanStatusCode = "Unchecked";
        private string _scanStatus;
        private string _scanMessage;
        private string _healthInsight;
        private string _searchText = string.Empty;
        private string _selectedFilterKey = FilterAll;
        private string _selectedFilter = string.Empty;

        private int _lastRecommendedServices;
        private int _lastSafeToOptimizeServices;
        private bool _hasScanResult;

        public ServicesViewModel()
        {
            _windowsServiceManager =
                new WindowsServiceManager();

            _windowsServiceController =
                new WindowsServiceController();

            _healthStateService =
                SystemHealthStateService.Instance;

            _startupTypeViewModel =
                new ServiceStartupTypeViewModel();

            _allServices =
                new List<WindowsServiceInfo>();

            Services =
                new ObservableCollection<WindowsServiceInfo>();

            ServicesHealth =
                new ServicesHealthSummary();

            AvailableFilters =
                new ObservableCollection<string>();

            _scanStatus =
                LocalizationHelper.Get(
                    "ServicesStatusUnchecked");

            _scanMessage =
                LocalizationHelper.Get(
                    "ServicesInitialMessage");

            _healthInsight =
                LocalizationHelper.Get(
                    "ServicesInsightInitial");

            RebuildFilters();

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
        {
            if (parameter is not
                WindowsServiceInfo service)
            {
                return;
            }

            if (!AdministratorRequirementHelper
                    .EnsureAdministrator())
            {
                return;
            }

            await StartServiceAsync(
                service);
        },
        parameter =>
            parameter is WindowsServiceInfo service &&
            service.CanStart &&
            !IsScanning);

            StopServiceCommand =
                new RelayCommand(
                    async parameter =>
                    {
                        if (parameter is not
                            WindowsServiceInfo service)
                        {
                            return;
                        }

                        if (!AdministratorRequirementHelper
                                .EnsureAdministrator())
                        {
                            return;
                        }

                        await StopServiceAsync(
                            service);
                    },
                    parameter =>
                        parameter is WindowsServiceInfo service &&
                        service.CanStop &&
                        !IsScanning);

            RestartServiceCommand =
                new RelayCommand(
                    async parameter =>
                    {
                        if (parameter is not
                            WindowsServiceInfo service)
                        {
                            return;
                        }

                        if (!AdministratorRequirementHelper
                                .EnsureAdministrator())
                        {
                            return;
                        }

                        await RestartServiceAsync(
                            service);
                    },
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

            if (!AdministratorRequirementHelper
                    .EnsureAdministrator())
            {
                service.CancelStartupTypeChange();

                CommandManager
                    .InvalidateRequerySuggested();

                return;
            }

            bool wasApplied =
                await _startupTypeViewModel
                    .ApplyStartupTypeAsync(
                        service);

            if (wasApplied)
            {
                ScanMessage =
                    LocalizationHelper.Format(
                        "ServicesStartupTypeChanged",
                        service.DisplayName,
                        service.StartType);

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

            LanguageManager.Instance.LanguageChanged +=
                LanguageManager_LanguageChanged;
        }

        public ObservableCollection<WindowsServiceInfo>
            Services
        {
            get;
        }

        public ServicesHealthSummary ServicesHealth
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
                OnPropertyChanged(
                    nameof(ScanButtonText));

                CommandManager
                    .InvalidateRequerySuggested();
            }
        }

        public string ScanStatusCode
        {
            get => _scanStatusCode;

            private set
            {
                if (_scanStatusCode == value)
                {
                    return;
                }

                _scanStatusCode = value;
                OnPropertyChanged();
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

        public string HealthInsight
        {
            get => _healthInsight;

            private set
            {
                if (_healthInsight == value)
                {
                    return;
                }

                _healthInsight = value;
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
                if (_selectedFilter == value ||
                    string.IsNullOrWhiteSpace(value))
                {
                    return;
                }

                _selectedFilter = value;
                _selectedFilterKey =
                    ResolveFilterKey(value);

                OnPropertyChanged();
                ApplyFilter();
            }
        }

        public string ScanButtonText =>
            IsScanning
                ? LocalizationHelper.Get(
                    "ServicesScanButtonBusy")
                : LocalizationHelper.Get(
                    "ServicesScanButtonIdle");

        private async Task ScanServicesAsync()
        {
            if (IsScanning)
            {
                return;
            }

            IsScanning = true;

            SetScanState(
                "Checking",
                "ServicesStatusChecking");

            ScanMessage =
                LocalizationHelper.Get(
                    "ServicesScanningMessage");

            try
            {
                List<WindowsServiceInfo> services =
                    await _windowsServiceManager
                        .GetServicesAsync();

                _allServices.Clear();
                _allServices.AddRange(services);

                UpdateServicesHealthScore(
                    services);

                ApplyFilter();

                SetScanState(
                    "Verified",
                    "ServicesStatusVerified");

                ScanMessage =
                    services.Count == 0
                        ? LocalizationHelper.Get(
                            "ServicesNoServicesFound")
                        : LocalizationHelper.Format(
                            "ServicesScanCompleted",
                            services.Count);
            }
            catch (Exception ex)
            {
                SetScanState(
                    "Error",
                    "ServicesStatusError");

                ScanMessage =
                    LocalizationHelper.Format(
                        "ServicesScanFailed",
                        ex.Message);
            }
            finally
            {
                IsScanning = false;
            }
        }

        private void UpdateServicesHealthScore(
            IReadOnlyCollection<WindowsServiceInfo> services)
        {
            int runningServices =
                services.Count(
                    service =>
                        service.Status.Equals(
                            "Running",
                            StringComparison.OrdinalIgnoreCase));

            int criticalServices =
                services.Count(
                    service =>
                        service.RiskLevel.Equals(
                            "Critical",
                            StringComparison.OrdinalIgnoreCase));

            int recommendedServices =
                services.Count(
                    service =>
                        service.RiskLevel.Equals(
                            "Medium",
                            StringComparison.OrdinalIgnoreCase));

            int safeToOptimizeServices =
                services.Count(
                    service =>
                        service.RiskLevel.Equals(
                            "Low",
                            StringComparison.OrdinalIgnoreCase));

            ServicesHealth.TotalServices =
                services.Count;

            ServicesHealth.RunningServices =
                runningServices;

            ServicesHealth.CriticalServices =
                criticalServices;

            ServicesHealth.RecommendedServices =
                recommendedServices;

            ServicesHealth.SafeToOptimizeServices =
                safeToOptimizeServices;

            ServicesHealth.EstimatedHealthGain =
                Math.Min(
                    25,
                    recommendedServices +
                    safeToOptimizeServices);

            _lastRecommendedServices =
                recommendedServices;

            _lastSafeToOptimizeServices =
                safeToOptimizeServices;

            _hasScanResult = true;

            UpdateHealthInsight();

            _healthStateService.UpdateServicesData(
                services.Count,
                criticalServices,
                recommendedServices,
                safeToOptimizeServices);

            int servicesScore = 100;

            servicesScore -=
                criticalServices * 20;

            servicesScore -=
                recommendedServices * 5;

            servicesScore -=
                safeToOptimizeServices * 2;

            servicesScore =
                Math.Clamp(
                    servicesScore,
                    0,
                    100);

            WinBoostHealthScoreService
                .Instance
                .ServicesScore =
                    servicesScore;

        }

        private async Task StartServiceAsync(
     WindowsServiceInfo? service)
        {
            if (service == null ||
                service.IsBusy)
            {
                return;
            }

            bool confirmed =
                NativeConfirmationDialog.Ask(
                    Application.Current.MainWindow,
                    LocalizationHelper.Get(
                        "ServicesStartDialogTitle"),
                    LocalizationHelper.Format(
                        "ServicesStartConfirmation",
                        service.DisplayName,
                        service.ServiceName),
                    LocalizationHelper.Get(
                        "WindowsUpdateYes"),
                    LocalizationHelper.Get(
                        "WindowsUpdateNo"));

            if (!confirmed)
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

                HandleOperationResult(
                    service,
                    result,
                    "Running");
            }
            finally
            {
                FinishServiceOperation(service);
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

            bool confirmed =
                NativeConfirmationDialog.Ask(
                    Application.Current.MainWindow,
                    LocalizationHelper.Get(
                        "ServicesStopDialogTitle"),
                    LocalizationHelper.Format(
                        "ServicesStopConfirmation",
                        service.DisplayName,
                        service.ServiceName),
                    LocalizationHelper.Get(
                        "WindowsUpdateYes"),
                    LocalizationHelper.Get(
                        "WindowsUpdateNo"));

            if (!confirmed)
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

                HandleOperationResult(
                    service,
                    result,
                    "Stopped");
            }
            finally
            {
                FinishServiceOperation(service);
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

            bool confirmed =
                NativeConfirmationDialog.Ask(
                    Application.Current.MainWindow,
                    LocalizationHelper.Get(
                        "ServicesRestartDialogTitle"),
                    LocalizationHelper.Format(
                        "ServicesRestartConfirmation",
                        service.DisplayName,
                        service.ServiceName),
                    LocalizationHelper.Get(
                        "WindowsUpdateYes"),
                    LocalizationHelper.Get(
                        "WindowsUpdateNo"));

            if (!confirmed)
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

                HandleOperationResult(
                    service,
                    result,
                    "Running");
            }
            finally
            {
                FinishServiceOperation(service);
            }
        }

        private void HandleOperationResult(
    WindowsServiceInfo service,
    ServiceOperationResult result,
    string fallbackStatus)
        {
            if (!result.IsSuccessful)
            {
                ShowOperationError(
                    service,
                    result.Message);

                return;
            }

            UpdateServiceStatus(
                service,
                string.IsNullOrWhiteSpace(
                    result.CurrentStatus)
                    ? fallbackStatus
                    : result.CurrentStatus);

            ScanMessage =
                LocalizationHelper.Format(
                    "ServicesOperationCompleted",
                    service.DisplayName,
                    result.Message);
        }

        private static void FinishServiceOperation(
            WindowsServiceInfo service)
        {
            service.IsBusy = false;

            CommandManager
                .InvalidateRequerySuggested();
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

            UpdateServicesHealthScore(
                _allServices);

            ApplyFilter();
        }

        private void ShowOperationError(
      WindowsServiceInfo service,
      string message)
        {
            string localizedMessage =
                GetLocalizedOperationError(
                    service,
                    message);

            NativeConfirmationDialog.ShowAcknowledgement(
                Application.Current.MainWindow,
                LocalizationHelper.Get(
                    "ServicesOperationErrorTitle"),
                localizedMessage,
                LocalizationHelper.Get(
                    "CommonCloseButton"));
        }

        private static string GetLocalizedOperationError(
            WindowsServiceInfo service,
            string message)
        {
            string sourceMessage =
                message ?? string.Empty;

            if (sourceMessage.Contains(
                    "access is denied",
                    StringComparison.OrdinalIgnoreCase) ||
                sourceMessage.Contains(
                    "access denied",
                    StringComparison.OrdinalIgnoreCase) ||
                sourceMessage.Contains(
                    "refuzat",
                    StringComparison.OrdinalIgnoreCase) ||
                sourceMessage.Contains(
                    "administrator",
                    StringComparison.OrdinalIgnoreCase))
            {
                return LocalizationHelper.Format(
                    "ServicesOperationErrorAccessDenied",
                    service.DisplayName);
            }

            if (sourceMessage.Contains(
                    "specified time interval",
                    StringComparison.OrdinalIgnoreCase) ||
                sourceMessage.Contains(
                    "timed out",
                    StringComparison.OrdinalIgnoreCase) ||
                sourceMessage.Contains(
                    "timeout",
                    StringComparison.OrdinalIgnoreCase) ||
                sourceMessage.Contains(
                    "did not respond",
                    StringComparison.OrdinalIgnoreCase))
            {
                return LocalizationHelper.Format(
                    "ServicesOperationErrorTimeout",
                    service.DisplayName);
            }

            if (sourceMessage.Contains(
                    "could not be started",
                    StringComparison.OrdinalIgnoreCase) ||
                sourceMessage.Contains(
                    "cannot start",
                    StringComparison.OrdinalIgnoreCase) ||
                sourceMessage.Contains(
                    "could not start",
                    StringComparison.OrdinalIgnoreCase))
            {
                return LocalizationHelper.Format(
                    "ServicesOperationErrorStart",
                    service.DisplayName);
            }

            if (sourceMessage.Contains(
                    "could not be stopped",
                    StringComparison.OrdinalIgnoreCase) ||
                sourceMessage.Contains(
                    "cannot stop",
                    StringComparison.OrdinalIgnoreCase) ||
                sourceMessage.Contains(
                    "could not stop",
                    StringComparison.OrdinalIgnoreCase))
            {
                return LocalizationHelper.Format(
                    "ServicesOperationErrorStop",
                    service.DisplayName);
            }

            return LocalizationHelper.Format(
                "ServicesOperationErrorGeneric",
                service.DisplayName);
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

        private void ApplyFilter()
        {
            IEnumerable<WindowsServiceInfo>
                filteredServices =
                    _allServices;

            if (!string.IsNullOrWhiteSpace(
                    SearchText))
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
                _selectedFilterKey switch
                {
                    FilterRunning =>
                        filteredServices.Where(
                            service =>
                                service.Status.Equals(
                                    "Running",
                                    StringComparison.OrdinalIgnoreCase)),

                    FilterStopped =>
                        filteredServices.Where(
                            service =>
                                !service.Status.Equals(
                                    "Running",
                                    StringComparison.OrdinalIgnoreCase)),

                    FilterAutomatic =>
                        filteredServices.Where(
                            service =>
                                service.StartType.Equals(
                                    "Automatic",
                                    StringComparison.OrdinalIgnoreCase)),

                    FilterAutomaticDelayed =>
                        filteredServices.Where(
                            service =>
                                service.StartType.Equals(
                                    "Automatic (Delayed)",
                                    StringComparison.OrdinalIgnoreCase)),

                    FilterManual =>
                        filteredServices.Where(
                            service =>
                                service.StartType.Equals(
                                    "Manual",
                                    StringComparison.OrdinalIgnoreCase)),

                    FilterDisabled =>
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

        private void UpdateHealthInsight()
        {
            if (!_hasScanResult)
            {
                HealthInsight =
                    LocalizationHelper.Get(
                        "ServicesInsightInitial");

                return;
            }

            int optimizableServices =
                _lastRecommendedServices +
                _lastSafeToOptimizeServices;

            HealthInsight =
                optimizableServices switch
                {
                    0 =>
                        LocalizationHelper.Get(
                            "ServicesInsightGood"),

                    1 =>
                        LocalizationHelper.Get(
                            "ServicesInsightSingle"),

                    _ =>
                        LocalizationHelper.Format(
                            "ServicesInsightMultiple",
                            optimizableServices)
                };
        }

        private void SetScanState(
            string statusCode,
            string resourceKey)
        {
            ScanStatusCode =
                statusCode;

            ScanStatus =
                LocalizationHelper.Get(
                    resourceKey);
        }

        private void RebuildFilters()
        {
            string selectedKey =
                _selectedFilterKey;

            AvailableFilters.Clear();

            AvailableFilters.Add(
                LocalizationHelper.Get(
                    "ServicesFilterAll"));

            AvailableFilters.Add(
                LocalizationHelper.Get(
                    "ServicesFilterRunning"));

            AvailableFilters.Add(
                LocalizationHelper.Get(
                    "ServicesFilterStopped"));

            AvailableFilters.Add(
                LocalizationHelper.Get(
                    "ServicesFilterAutomatic"));

            AvailableFilters.Add(
                LocalizationHelper.Get(
                    "ServicesFilterAutomaticDelayed"));

            AvailableFilters.Add(
                LocalizationHelper.Get(
                    "ServicesFilterManual"));

            AvailableFilters.Add(
                LocalizationHelper.Get(
                    "ServicesFilterDisabled"));

            _selectedFilter =
                GetFilterDisplayText(
                    selectedKey);

            OnPropertyChanged(
                nameof(SelectedFilter));
        }

        private string ResolveFilterKey(
            string displayText)
        {
            if (displayText ==
                LocalizationHelper.Get(
                    "ServicesFilterRunning"))
            {
                return FilterRunning;
            }

            if (displayText ==
                LocalizationHelper.Get(
                    "ServicesFilterStopped"))
            {
                return FilterStopped;
            }

            if (displayText ==
                LocalizationHelper.Get(
                    "ServicesFilterAutomatic"))
            {
                return FilterAutomatic;
            }

            if (displayText ==
                LocalizationHelper.Get(
                    "ServicesFilterAutomaticDelayed"))
            {
                return FilterAutomaticDelayed;
            }

            if (displayText ==
                LocalizationHelper.Get(
                    "ServicesFilterManual"))
            {
                return FilterManual;
            }

            if (displayText ==
                LocalizationHelper.Get(
                    "ServicesFilterDisabled"))
            {
                return FilterDisabled;
            }

            return FilterAll;
        }

        private string GetFilterDisplayText(
            string filterKey)
        {
            return filterKey switch
            {
                FilterRunning =>
                    LocalizationHelper.Get(
                        "ServicesFilterRunning"),

                FilterStopped =>
                    LocalizationHelper.Get(
                        "ServicesFilterStopped"),

                FilterAutomatic =>
                    LocalizationHelper.Get(
                        "ServicesFilterAutomatic"),

                FilterAutomaticDelayed =>
                    LocalizationHelper.Get(
                        "ServicesFilterAutomaticDelayed"),

                FilterManual =>
                    LocalizationHelper.Get(
                        "ServicesFilterManual"),

                FilterDisabled =>
                    LocalizationHelper.Get(
                        "ServicesFilterDisabled"),

                _ =>
                    LocalizationHelper.Get(
                        "ServicesFilterAll")
            };
        }

        private void LanguageManager_LanguageChanged(
            object? sender,
            EventArgs e)
        {
            ScanStatus =
                LocalizationHelper.Get(
                    ScanStatusCode switch
                    {
                        "Checking" =>
                            "ServicesStatusChecking",

                        "Verified" =>
                            "ServicesStatusVerified",

                        "Error" =>
                            "ServicesStatusError",

                        _ =>
                            "ServicesStatusUnchecked"
                    });

            if (!_hasScanResult &&
                !IsScanning)
            {
                ScanMessage =
                    LocalizationHelper.Get(
                        "ServicesInitialMessage");
            }
            else if (IsScanning)
            {
                ScanMessage =
                    LocalizationHelper.Get(
                        "ServicesScanningMessage");
            }

            RebuildFilters();
            UpdateHealthInsight();
            ServicesHealth.RefreshLocalizedText();

            OnPropertyChanged(
                nameof(ScanButtonText));

            ApplyFilter();
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