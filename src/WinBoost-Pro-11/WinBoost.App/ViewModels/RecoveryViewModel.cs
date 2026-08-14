using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WinBoost.App.Commands;
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

        private bool _isCheckingAvailability;

        private bool _isCreatingRestorePoint;

        private bool _isEnablingSystemProtection;

        private bool _isLoadingRestorePoints;

        private bool _isRestoringSystem;

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

            CreateRestorePointCommand =
                _createRestorePointCommand;

            EnableSystemProtectionCommand =
                _enableSystemProtectionCommand;

            RestoreSystemCommand =
                _restoreSystemCommand;
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

                OnPropertyChanged(
                    nameof(CanRestoreSystem));

                _restoreSystemCommand
                    .RaiseCanExecuteChanged();
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
            !IsRestoringSystem;


        public bool CanEnableSystemProtection =>
            IsSystemRestoreAvailable &&
            IsSystemProtectionActionRequired &&
            !IsCheckingAvailability &&
            !IsCreatingRestorePoint &&
            !IsEnablingSystemProtection &&
            !IsLoadingRestorePoints &&
            !IsRestoringSystem;


        public bool CanRestoreSystem =>
            IsSystemRestoreAvailable &&
            SelectedRestorePoint != null &&
            !IsCheckingAvailability &&
            !IsCreatingRestorePoint &&
            !IsEnablingSystemProtection &&
            !IsLoadingRestorePoints &&
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


        public async Task CheckAvailabilityAsync()
        {
            if (IsCheckingAvailability)
            {
                return;
            }

            IsCheckingAvailability =
                true;

            AvailabilityMessage =
                "Checking System Restore availability...";

            try
            {
                SystemRestoreAvailabilityResult result =
                    await _restorePointService
                        .CheckAvailabilityAsync();

                IsSystemRestoreAvailable =
                    result.IsAvailable;

                AvailabilityMessage =
                    result.Message;
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


        public async Task LoadRestorePointsAsync()
        {
            if (IsLoadingRestorePoints)
            {
                return;
            }

            IsLoadingRestorePoints =
                true;

            RestorePointsMessage =
                "Loading restore points...";

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
                        ? "No restore points were found."
                        : $"{RestorePoints.Count} restore point(s) found.";
            }
            catch (Exception ex)
            {
                RestorePoints.Clear();

                SelectedRestorePoint =
                    null;

                RestorePointsMessage =
                    $"Could not load restore points: {ex.Message}";
            }
            finally
            {
                IsLoadingRestorePoints =
                    false;
            }
        }


        private async Task CreateRestorePointAsync()
        {
            if (!CanCreateRestorePoint)
            {
                return;
            }

            IsCreatingRestorePoint =
                true;

            StatusMessage =
                "Creating system restore point...";

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
                        $"Restore point created successfully. " +
                        $"Created at: {createdAt:dd.MM.yyyy HH:mm:ss}";

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
                        "System Protection is disabled for the system drive. " +
                        "Enable System Protection before creating a restore point.";

                    return;
                }

                StatusMessage =
                    result.Message;
            }
            catch (Exception ex)
            {
                StatusMessage =
                    $"Restore point error: {ex.Message}";
            }
            finally
            {
                IsCreatingRestorePoint =
                    false;
            }
        }


        private async Task EnableSystemProtectionAsync()
        {
            if (!CanEnableSystemProtection)
            {
                return;
            }

            IsEnablingSystemProtection =
                true;

            StatusMessage =
                "Enabling System Protection...";

            try
            {
                SystemProtectionResult result =
                    await _restorePointService
                        .EnableSystemProtectionAsync();

                if (!result.IsSuccessful)
                {
                    StatusMessage =
                        result.Message;

                    return;
                }

                StatusMessage =
                    "System Protection was enabled successfully.";

                IsSystemProtectionActionRequired =
                    false;

                await Task.Delay(
                    TimeSpan.FromSeconds(2));

                await CheckAvailabilityAsync();
            }
            catch (Exception ex)
            {
                StatusMessage =
                    $"System Protection error: {ex.Message}";
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
                    "No restore point selected.";

                return;
            }

            MessageBoxResult confirmation =
                MessageBox.Show(
                    $"You are about to restore Windows to:\n\n" +
                    $"{selectedRestorePoint.CreatedAtDisplay}\n" +
                    $"{selectedRestorePoint.Description}\n\n" +
                    "Applications, drivers and system settings installed " +
                    "after this restore point may be affected.\n\n" +
                    "Do you want to continue?",
                    "WinBoost Pro 11 - System Restore",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

            if (confirmation !=
                MessageBoxResult.Yes)
            {
                StatusMessage =
                    "System Restore was cancelled.";

                return;
            }

            IsRestoringSystem =
                true;

            StatusMessage =
                "Starting System Restore...";

            try
            {
                SystemRestorePointResult result =
                    await _restorePointService
                        .RestoreSystemAsync(
                            selectedRestorePoint.SequenceNumber);

                if (!result.IsSuccessful)
                {
                    StatusMessage =
                        result.Message;

                    return;
                }

                StatusMessage =
                    result.Message;

                MessageBox.Show(
                    "System Restore was started successfully.\n\n" +
                    "Windows must be restarted to complete the restoration.\n\n" +
                    "WinBoost will not restart the computer automatically yet.",
                    "WinBoost Pro 11 - System Restore",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage =
                    $"System Restore error: {ex.Message}";
            }
            finally
            {
                IsRestoringSystem =
                    false;
            }
        }


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


        private void NotifyCommandStates()
        {
            OnPropertyChanged(
                nameof(CanCreateRestorePoint));

            OnPropertyChanged(
                nameof(CanEnableSystemProtection));

            OnPropertyChanged(
                nameof(CanRestoreSystem));

            _createRestorePointCommand
                .RaiseCanExecuteChanged();

            _enableSystemProtectionCommand
                .RaiseCanExecuteChanged();

            _restoreSystemCommand
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