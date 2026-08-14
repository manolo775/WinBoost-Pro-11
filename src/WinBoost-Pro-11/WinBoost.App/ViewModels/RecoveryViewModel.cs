using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WinBoost.App.Commands;
using WinBoost.App.Helpers;
using WinBoost.App.Localization;
using WinBoost.App.Models;
using WinBoost.App.Services.Recovery;


namespace WinBoost.App.ViewModels
{
    public sealed class RecoveryViewModel : INotifyPropertyChanged
    {
        private const uint ServiceDisabledHResult =
            0x80070422u;

        private readonly SystemRestorePointService
            _restorePointService;

        private readonly SystemRestorePointScanner
            _restorePointScanner;

        private readonly AsyncRelayCommand
            _createRestorePointCommand;

        private readonly AsyncRelayCommand
            _enableSystemProtectionCommand;

        private readonly AsyncRelayCommand
            _restoreSystemCommand;

        private readonly AsyncRelayCommand
            _restartNowCommand;

        private readonly AsyncRelayCommand
            _restartLaterCommand;

        private bool _isCheckingAvailability;

        private bool _isCreatingRestorePoint;

        private bool _isEnablingSystemProtection;

        private bool _isLoadingRestorePoints;

        private bool _isRestoringSystem;

        private bool _isRestartRequired;

        private bool _isSystemRestoreAvailable;

        private bool _isSystemProtectionActionRequired;

        private SystemRestorePointInfo?
            _selectedRestorePoint;

        private string _availabilityMessage =
            string.Empty;

        private string _statusMessage =
            string.Empty;

        private string _restorePointsMessage =
            string.Empty;


        public RecoveryViewModel()
        {
            _restorePointService =
                new SystemRestorePointService();

            _restorePointScanner =
                new SystemRestorePointScanner();

            RestorePoints =
                new ObservableCollection<SystemRestorePointInfo>();

            _createRestorePointCommand =
                new AsyncRelayCommand(
                    CreateRestorePointAsync,
                    () =>
                        CanCreateRestorePoint);

            _enableSystemProtectionCommand =
                new AsyncRelayCommand(
                    EnableSystemProtectionAsync,
                    () =>
                        CanEnableSystemProtection);

            _restoreSystemCommand =
                new AsyncRelayCommand(
                    RestoreSystemAsync,
                    () =>
                        CanRestoreSystem);

            _restartNowCommand =
                new AsyncRelayCommand(
                    RestartNowAsync,
                    () =>
                        CanRestartNow);

            _restartLaterCommand =
                new AsyncRelayCommand(
                    RestartLaterAsync,
                    () =>
                        CanRestartLater);

            CreateRestorePointCommand =
                _createRestorePointCommand;

            EnableSystemProtectionCommand =
                _enableSystemProtectionCommand;

            RestoreSystemCommand =
                _restoreSystemCommand;

            RestartNowCommand =
                _restartNowCommand;

            RestartLaterCommand =
                _restartLaterCommand;

            WeakEventManager<LanguageManager, EventArgs>
             .AddHandler(
             LanguageManager.Instance,
              nameof(LanguageManager.LanguageChanged),
              OnLanguageChanged);

        }


        public ICommand CreateRestorePointCommand
        {
            get;
        }

        public ICommand EnableSystemProtectionCommand
        {
            get;
        }

        public ICommand RestoreSystemCommand
        {
            get;
        }

        public ICommand RestartNowCommand
        {
            get;
        }

        public ICommand RestartLaterCommand
        {
            get;
        }


        public ObservableCollection<SystemRestorePointInfo>
            RestorePoints
        {
            get;
        }


        public SystemRestorePointInfo?
            SelectedRestorePoint
        {
            get => _selectedRestorePoint;

            set
            {
                if (_selectedRestorePoint == value)
                {
                    return;
                }

                _selectedRestorePoint =
                    value;

                OnPropertyChanged();



                NotifyCommandStates();
            }
        }


        public bool IsCheckingAvailability
        {
            get => _isCheckingAvailability;

            private set
            {
                if (_isCheckingAvailability == value)
                {
                    return;
                }

                _isCheckingAvailability =
                    value;

                OnPropertyChanged();

                NotifyCommandStates();
            }
        }


        public bool IsCreatingRestorePoint
        {
            get => _isCreatingRestorePoint;

            private set
            {
                if (_isCreatingRestorePoint == value)
                {
                    return;
                }

                _isCreatingRestorePoint =
                    value;

                OnPropertyChanged();

                NotifyCommandStates();
            }
        }


        public bool IsEnablingSystemProtection
        {
            get => _isEnablingSystemProtection;

            private set
            {
                if (_isEnablingSystemProtection == value)
                {
                    return;
                }

                _isEnablingSystemProtection =
                    value;

                OnPropertyChanged();

                NotifyCommandStates();
            }
        }


        public bool IsLoadingRestorePoints
        {
            get => _isLoadingRestorePoints;

            private set
            {
                if (_isLoadingRestorePoints == value)
                {
                    return;
                }

                _isLoadingRestorePoints =
                    value;

                OnPropertyChanged();

                NotifyCommandStates();
            }
        }


        public bool IsRestoringSystem
        {
            get => _isRestoringSystem;

            private set
            {
                if (_isRestoringSystem == value)
                {
                    return;
                }

                _isRestoringSystem =
                    value;

                OnPropertyChanged();

                NotifyCommandStates();
            }
        }


        public bool IsRestartRequired
        {
            get => _isRestartRequired;

            private set
            {
                if (_isRestartRequired == value)
                {
                    return;
                }

                _isRestartRequired =
                    value;

                OnPropertyChanged();

                NotifyCommandStates();
            }
        }


        public bool IsSystemRestoreAvailable
        {
            get => _isSystemRestoreAvailable;

            private set
            {
                if (_isSystemRestoreAvailable == value)
                {
                    return;
                }

                _isSystemRestoreAvailable =
                    value;

                OnPropertyChanged();

                NotifyCommandStates();
            }
        }


        public bool IsSystemProtectionActionRequired
        {
            get => _isSystemProtectionActionRequired;

            private set
            {
                if (_isSystemProtectionActionRequired == value)
                {
                    return;
                }

                _isSystemProtectionActionRequired =
                    value;

                OnPropertyChanged();

                NotifyCommandStates();
            }
        }


        public bool CanCreateRestorePoint =>
            IsSystemRestoreAvailable &&
            !IsCheckingAvailability &&
            !IsCreatingRestorePoint &&
            !IsEnablingSystemProtection &&
            !IsLoadingRestorePoints &&
            !IsRestoringSystem &&
            !IsRestartRequired;


        public bool CanEnableSystemProtection =>
            IsSystemRestoreAvailable &&
            IsSystemProtectionActionRequired &&
            !IsCheckingAvailability &&
            !IsCreatingRestorePoint &&
            !IsEnablingSystemProtection &&
            !IsLoadingRestorePoints &&
            !IsRestoringSystem &&
            !IsRestartRequired;


        public bool CanRestoreSystem =>
            IsSystemRestoreAvailable &&
            SelectedRestorePoint != null &&
            !IsCheckingAvailability &&
            !IsCreatingRestorePoint &&
            !IsEnablingSystemProtection &&
            !IsLoadingRestorePoints &&
            !IsRestoringSystem &&
            !IsRestartRequired;


        public bool CanRestartNow =>
            IsRestartRequired &&
            !IsRestoringSystem;


        public bool CanRestartLater =>
            IsRestartRequired &&
            !IsRestoringSystem;


        public string AvailabilityMessage
        {
            get => _availabilityMessage;

            private set
            {
                if (_availabilityMessage == value)
                {
                    return;
                }

                _availabilityMessage =
                    value;

                OnPropertyChanged();
            }
        }


        public string StatusMessage
        {
            get => _statusMessage;

            private set
            {
                if (_statusMessage == value)
                {
                    return;
                }

                _statusMessage =
                    value;

                OnPropertyChanged();
            }
        }


        public string RestorePointsMessage
        {
            get => _restorePointsMessage;

            private set
            {
                if (_restorePointsMessage == value)
                {
                    return;
                }

                _restorePointsMessage =
                    value;

                OnPropertyChanged();
            }
        }


        // ======================================
        // CHECK AVAILABILITY
        // ======================================

        public async Task CheckAvailabilityAsync()
        {
            if (IsCheckingAvailability)
            {
                return;
            }

            IsCheckingAvailability =
                true;

            AvailabilityMessage =
                LocalizationHelper.Get(
                    "RecoveryCheckingAvailability");

            try
            {
                SystemRestoreAvailabilityResult result =
                    await _restorePointService
                        .CheckAvailabilityAsync();

                IsSystemRestoreAvailable =
                    result.IsAvailable;

                /*
                 * Mesajul serviciului rămâne folosit momentan
                 * deoarece conține și eventualele detalii WMI.
                 */
                AvailabilityMessage =
                          result.IsAvailable
                         ? LocalizationHelper.Get(
                          "RecoverySystemRestoreAvailable")
                         : LocalizationHelper.Get(
                          "RecoverySystemRestoreUnavailable");
            }
            catch (Exception ex)
            {
                IsSystemRestoreAvailable =
                    false;

                AvailabilityMessage =
                    ex.Message;
            }
            finally
            {
                IsCheckingAvailability =
                    false;
            }
        }


        // ======================================
        // LOAD RESTORE POINTS
        // ======================================

        public async Task LoadRestorePointsAsync()
        {
            if (IsLoadingRestorePoints)
            {
                return;
            }

            IsLoadingRestorePoints =
                true;

            RestorePointsMessage =
                LocalizationHelper.Get(
                    "RecoveryLoadingRestorePoints");

            try
            {
                var restorePoints =
                    await _restorePointScanner
                        .ScanAsync();

                RestorePoints.Clear();

                foreach (SystemRestorePointInfo
                         restorePoint in restorePoints)
                {
                    RestorePoints.Add(
                        restorePoint);
                }

                SelectedRestorePoint =
                    RestorePoints.Count > 0
                        ? RestorePoints[0]
                        : null;

                RestorePointsMessage =
                    RestorePoints.Count == 0
                        ? LocalizationHelper.Get(
                            "RecoveryNoRestorePoints")
                        : LocalizationHelper.Format(
                            "RecoveryRestorePointsFound",
                            RestorePoints.Count);
            }
            catch (Exception ex)
            {
                RestorePoints.Clear();

                SelectedRestorePoint =
                    null;

                RestorePointsMessage =
                    LocalizationHelper.Format(
                        "RecoveryRestorePointsLoadFailed",
                        ex.Message);
            }
            finally
            {
                IsLoadingRestorePoints =
                    false;
            }
        }


        // ======================================
        // CREATE RESTORE POINT
        // ======================================

        private async Task CreateRestorePointAsync()
        {
            if (!CanCreateRestorePoint)
            {
                return;
            }

            IsCreatingRestorePoint =
                true;

            StatusMessage =
                LocalizationHelper.Get(
                    "RecoveryCreatingRestorePoint");

            try
            {
                DateTime createdAt =
                    DateTime.Now;

                string restorePointDescription =
                    $"WinBoost Pro 11 - Safety Restore Point - " +
                    $"{createdAt:dd.MM.yyyy HH:mm:ss}";

                SystemRestorePointResult result =
                    await _restorePointService
                        .CreateRestorePointAsync(
                            restorePointDescription);

                if (result.IsSuccessful)
                {
                    IsSystemProtectionActionRequired =
                        false;

                    StatusMessage =
                        LocalizationHelper.Format(
                            "RecoveryRestorePointCreatedWithDate",
                            createdAt.ToString(
                                "dd.MM.yyyy HH:mm:ss"));

                    await Task.Delay(
                        TimeSpan.FromSeconds(2));

                    await LoadRestorePointsAsync();

                    return;
                }

                if (IsSystemProtectionDisabledResult(
                        result))
                {
                    IsSystemProtectionActionRequired =
                        true;

                    StatusMessage =
                        LocalizationHelper.Get(
                            "RecoverySystemProtectionDisabled");

                    return;
                }

                if (result.Message ==
       "RECOVERY_NO_NEW_RESTORE_POINT")
                {
                    StatusMessage =
                        LocalizationHelper.Get(
                            "RecoveryNoNewRestorePoint");
                }
                else
                {
                    StatusMessage =
                        LocalizationHelper.Format(
                            "RecoveryRestorePointCreateFailed",
                            result.Message);
                }
            }
            catch (Exception ex)
            {
                StatusMessage =
                    LocalizationHelper.Format(
                        "RecoveryRestorePointError",
                        ex.Message);
            }
            finally
            {
                IsCreatingRestorePoint =
                    false;
            }
        }


        // ======================================
        // ENABLE SYSTEM PROTECTION
        // ======================================

        private async Task EnableSystemProtectionAsync()
        {
            if (!CanEnableSystemProtection)
            {
                return;
            }

            IsEnablingSystemProtection =
                true;

            StatusMessage =
                LocalizationHelper.Get(
                    "RecoveryEnablingSystemProtection");

            try
            {
                SystemProtectionResult result =
                    await _restorePointService
                        .EnableSystemProtectionAsync();

                if (!result.IsSuccessful)
                {
                    StatusMessage =
                        LocalizationHelper.Format(
                            "RecoverySystemProtectionError",
                            result.Message);

                    return;
                }

                StatusMessage =
                    LocalizationHelper.Get(
                        "RecoverySystemProtectionEnabled");

                IsSystemProtectionActionRequired =
                    false;

                await Task.Delay(
                    TimeSpan.FromSeconds(2));

                await CheckAvailabilityAsync();
            }
            catch (Exception ex)
            {
                StatusMessage =
                    LocalizationHelper.Format(
                        "RecoverySystemProtectionError",
                        ex.Message);
            }
            finally
            {
                IsEnablingSystemProtection =
                    false;
            }
        }


        // ======================================
        // RESTORE SELECTED RESTORE POINT
        // ======================================

        private async Task RestoreSystemAsync()
        {
            SystemRestorePointInfo?
                selectedRestorePoint =
                    SelectedRestorePoint;

            if (selectedRestorePoint == null)
            {
                StatusMessage =
                    LocalizationHelper.Get(
                        "RecoveryNoRestorePointSelected");

                return;
            }

            string confirmationMessage =
                LocalizationHelper.Format(
                    "RecoveryRestoreDialogMessage",
                    selectedRestorePoint.CreatedAtDisplay,
                    selectedRestorePoint.Description);

            string confirmationTitle =
                LocalizationHelper.Get(
                    "RecoveryRestoreDialogTitle");

            MessageBoxResult confirmation =
                MessageBox.Show(
                    confirmationMessage,
                    confirmationTitle,
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

            if (confirmation !=
                MessageBoxResult.Yes)
            {
                StatusMessage =
                    LocalizationHelper.Get(
                        "RecoveryRestoreCancelled");

                return;
            }

            IsRestoringSystem =
                true;

            StatusMessage =
                LocalizationHelper.Get(
                    "RecoveryStartingRestore");

            try
            {
                SystemRestorePointResult result =
                    await _restorePointService
                        .RestoreSystemAsync(
                            selectedRestorePoint.SequenceNumber);

                if (!result.IsSuccessful)
                {
                    IsRestartRequired =
                        false;

                    StatusMessage =
                        LocalizationHelper.Format(
                            "RecoveryRestoreFailed",
                            result.Message);

                    return;
                }

                /*
                 * Windows a acceptat restaurarea.
                 * Calculatorul NU este repornit automat aici.
                 */

                IsRestartRequired =
                    true;

                StatusMessage =
                    LocalizationHelper.Get(
                        "RecoveryRestorePrepared");
            }
            catch (Exception ex)
            {
                IsRestartRequired =
                    false;

                StatusMessage =
                    LocalizationHelper.Format(
                        "RecoveryRestoreError",
                        ex.Message);
            }
            finally
            {
                IsRestoringSystem =
                    false;
            }
        }


        // ======================================
        // RESTART NOW
        // ======================================

        private Task RestartNowAsync()
        {
            if (!CanRestartNow)
            {
                return Task.CompletedTask;
            }

            string confirmationMessage =
                LocalizationHelper.Get(
                    "RecoveryRestartDialogMessage");

            string confirmationTitle =
                LocalizationHelper.Get(
                    "RecoveryRestartDialogTitle");

            MessageBoxResult confirmation =
                MessageBox.Show(
                    confirmationMessage,
                    confirmationTitle,
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

            if (confirmation !=
                MessageBoxResult.Yes)
            {
                StatusMessage =
                    LocalizationHelper.Get(
                        "RecoveryRestartCancelled");

                return Task.CompletedTask;
            }

            try
            {
                StatusMessage =
                    LocalizationHelper.Get(
                        "RecoveryRestarting");

                var startInfo =
                    new ProcessStartInfo
                    {
                        FileName =
                            "shutdown.exe",

                        Arguments =
                            "/r /t 0",

                        UseShellExecute =
                            false,

                        CreateNoWindow =
                            true
                    };

                Process.Start(
                    startInfo);
            }
            catch (Exception ex)
            {
                StatusMessage =
                    LocalizationHelper.Format(
                        "RecoveryWindowsRestartError",
                        ex.Message);
            }

            return Task.CompletedTask;
        }


        // ======================================
        // RESTART LATER
        // ======================================

        private Task RestartLaterAsync()
        {
            if (!CanRestartLater)
            {
                return Task.CompletedTask;
            }

            /*
             * Nu schimbăm IsRestartRequired în false.
             *
             * Restaurarea rămâne pregătită, iar zona
             * Restart required trebuie să rămână vizibilă.
             */

            StatusMessage =
                LocalizationHelper.Get(
                    "RecoveryRestartLaterMessage");

            return Task.CompletedTask;
        }


        // ======================================
        // SYSTEM PROTECTION ERROR DETECTION
        // ======================================

        private static bool
            IsSystemProtectionDisabledResult(
                SystemRestorePointResult result)
        {
            if (result.ReturnCode ==
                ServiceDisabledHResult)
            {
                return true;
            }

            return result.Message.Contains(
                "0x80070422",
                StringComparison.OrdinalIgnoreCase);
        }


        // ======================================
        // COMMAND STATES
        // ======================================

        private async void OnLanguageChanged(
            object? sender,
              EventArgs e)
        {
            /*
             * Refacem mesajele dinamice atunci când
             * utilizatorul schimbă limba RO / EN.
             */

            if (IsSystemRestoreAvailable)
            {
                AvailabilityMessage =
                    LocalizationHelper.Get(
                        "RecoverySystemRestoreAvailable");
            }
            else
            {
                AvailabilityMessage =
                    LocalizationHelper.Get(
                        "RecoverySystemRestoreUnavailable");
            }

            if (!IsLoadingRestorePoints)
            {
                RestorePointsMessage =
                    RestorePoints.Count == 0
                        ? LocalizationHelper.Get(
                            "RecoveryNoRestorePoints")
                        : LocalizationHelper.Format(
                            "RecoveryRestorePointsFound",
                            RestorePoints.Count);
            }

            if (IsRestartRequired)
            {
                StatusMessage =
                    LocalizationHelper.Get(
                        "RecoveryRestorePrepared");
            }
            else
            {
                StatusMessage =
                    string.Empty;
            }

            /*
             * Reîncărcăm punctele de restaurare pentru ca
             * DisplayDescription și DisplayRestorePointTypeName
             * să fie recalculate în limba nou selectată.
             */

            if (!IsLoadingRestorePoints)
            {
                await LoadRestorePointsAsync();
            }
        }
        private void NotifyCommandStates()
        {
            OnPropertyChanged(
                nameof(CanCreateRestorePoint));

            OnPropertyChanged(
                nameof(CanEnableSystemProtection));

            OnPropertyChanged(
                nameof(CanRestoreSystem));

            OnPropertyChanged(
                nameof(CanRestartNow));

            OnPropertyChanged(
                nameof(CanRestartLater));

            _createRestorePointCommand
                .RaiseCanExecuteChanged();

            _enableSystemProtectionCommand
                .RaiseCanExecuteChanged();

            _restoreSystemCommand
                .RaiseCanExecuteChanged();

            _restartNowCommand
                .RaiseCanExecuteChanged();

            _restartLaterCommand
                .RaiseCanExecuteChanged();
        }


        public event PropertyChangedEventHandler?
            PropertyChanged;


        private void OnPropertyChanged(
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