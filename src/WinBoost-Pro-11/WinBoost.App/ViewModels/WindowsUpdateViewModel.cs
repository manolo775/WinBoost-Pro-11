using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WinBoost.App.Commands;
using WinBoost.App.Localization;
using WinBoost.App.Services.WindowsUpdate;
using WinBoost.App.Helpers;

namespace WinBoost.App.ViewModels
{
    public sealed class WindowsUpdateAvailableDisplayItem
    {
        public string Title
        {
            get;
            init;
        } = string.Empty;

        public string Description
        {
            get;
            init;
        } = string.Empty;

        public string IsDownloaded
        {
            get;
            init;
        } = string.Empty;

        public string RebootRequired
        {
            get;
            init;
        } = string.Empty;
    }

    public class WindowsUpdateViewModel : INotifyPropertyChanged
    {
        private readonly WindowsUpdateScanner
            _windowsUpdateScanner;

        private readonly WindowsUpdateAvailableScanner
            _windowsUpdateAvailableScanner;

        private bool _isScanning;

        private string _scanStatus =
            string.Empty;

        private string _scanBadgeText =
            string.Empty;

        private string _scanState =
            "NotChecked";

        private WindowsUpdateScanResult?
            _lastScanResult;

        private IReadOnlyList<WindowsUpdateAvailableInfo>
            _lastAvailableUpdates =
                Array.Empty<WindowsUpdateAvailableInfo>();

        private int _lastAvailableUpdateCount;

        private string _lastErrorMessage =
            string.Empty;

        private readonly WindowsUpdateWorkerStatusReader
    _workerStatusReader =
        new WindowsUpdateWorkerStatusReader();

        private bool _isInstallingUpdates;

        private string _installationState =
            "Idle";

        private int _installationPercent;

        private string _installationMessage =
            string.Empty;

        private string _currentUpdateTitle =
            string.Empty;

        private bool _rebootRequired;

        public WindowsUpdateViewModel()
        {
            _windowsUpdateScanner =
                new WindowsUpdateScanner();

            _windowsUpdateAvailableScanner =
                new WindowsUpdateAvailableScanner();

            AvailableUpdates =
                new ObservableCollection<
                    WindowsUpdateAvailableDisplayItem>();

            ScanUpdatesCommand =
                new RelayCommand(
                    async _ =>
                        await ScanUpdatesAsync(),
                    _ =>
                        !IsScanning);

            InstallUpdatesCommand =
     new RelayCommand(
         async _ =>
             await ConfirmInstallUpdatesAsync(),
         _ =>
             CanInstallUpdates);

            ApplyInitialUiState();

            LanguageManager.Instance.LanguageChanged +=
                LanguageManager_LanguageChanged;
        }

        public ICommand ScanUpdatesCommand
        {
            get;
        }

        public ICommand InstallUpdatesCommand
        {
            get;
        }

        public ObservableCollection<
            WindowsUpdateAvailableDisplayItem>
            AvailableUpdates
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

                _scanStatus =
                    value;

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

                _scanBadgeText =
                    value;

                OnPropertyChanged();
            }
        }

        public string ScanState
        {
            get => _scanState;

            private set
            {
                if (_scanState == value)
                {
                    return;
                }

                _scanState =
                    value;

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

                _isScanning =
                    value;

                OnPropertyChanged();

                OnPropertyChanged(
                    nameof(ScanButtonText));

                OnPropertyChanged(
                    nameof(CanInstallUpdates));

                CommandManager
                    .InvalidateRequerySuggested();
            }
        }

        public bool IsInstallingUpdates
        {
            get => _isInstallingUpdates;

            private set
            {
                if (_isInstallingUpdates == value)
                {
                    return;
                }

                _isInstallingUpdates =
                    value;

                OnPropertyChanged();

                OnPropertyChanged(
                    nameof(CanInstallUpdates));

                CommandManager
                    .InvalidateRequerySuggested();
            }
        }

        public string InstallationState
        {
            get => _installationState;

            private set
            {
                if (_installationState == value)
                {
                    return;
                }

                _installationState =
                    value;

                OnPropertyChanged();
            }
        }

        public int InstallationPercent
        {
            get => _installationPercent;

            private set
            {
                int normalizedValue =
                    Math.Clamp(
                        value,
                        0,
                        100);

                if (_installationPercent ==
                    normalizedValue)
                {
                    return;
                }

                _installationPercent =
                    normalizedValue;

                OnPropertyChanged();
            }
        }

        public string InstallationMessage
        {
            get => _installationMessage;

            private set
            {
                if (_installationMessage == value)
                {
                    return;
                }

                _installationMessage =
                    value;

                OnPropertyChanged();
            }
        }

        public string CurrentUpdateTitle
        {
            get => _currentUpdateTitle;

            private set
            {
                if (_currentUpdateTitle == value)
                {
                    return;
                }

                _currentUpdateTitle =
                    value;

                OnPropertyChanged();
            }
        }

        public bool RebootRequired
        {
            get => _rebootRequired;

            private set
            {
                if (_rebootRequired == value)
                {
                    return;
                }

                _rebootRequired =
                    value;

                OnPropertyChanged();
            }
        }

        public bool CanInstallUpdates =>
       !IsScanning &&
       !IsInstallingUpdates &&
       _lastAvailableUpdateCount > 0;

        public string ScanButtonText =>
            IsScanning
                ? LocalizationHelper.Get(
                    "WindowsUpdateScanningButton")
                : LocalizationHelper.Get(
                    "WindowsUpdateScanButton");

        public string InstallButtonText =>
            LocalizationHelper.Get(
                "WindowsUpdateInstallButton");

        private async Task ScanUpdatesAsync()
        {
            if (IsScanning)
            {
                return;
            }

            IsScanning = true;

            ScanState =
                "Checking";

            ScanBadgeText =
                LocalizationHelper.Get(
                    "WindowsUpdateBadgeChecking");

            ScanStatus =
                LocalizationHelper.Get(
                    "WindowsUpdateScanningStatus");

            _lastErrorMessage =
                string.Empty;

            try
            {
                Task<WindowsUpdateScanResult>
                    servicesScanTask =
                        _windowsUpdateScanner
                            .ScanAsync();

                Task<WindowsUpdateAvailableResult>
                    updatesScanTask =
                        _windowsUpdateAvailableScanner
                            .ScanAsync();

                await Task.WhenAll(
                    servicesScanTask,
                    updatesScanTask);

                WindowsUpdateScanResult result =
                    await servicesScanTask;

                WindowsUpdateAvailableResult availableResult =
                    await updatesScanTask;

                _lastAvailableUpdates =
                    availableResult.Updates;

                RefreshAvailableUpdates();

                _lastScanResult =
                    result;

                _lastAvailableUpdateCount =
                    availableResult.UpdateCount;

                OnPropertyChanged(
                    nameof(CanInstallUpdates));

                CommandManager
                    .InvalidateRequerySuggested();

                ApplyScanResult(
                    result,
                    availableResult.UpdateCount);
            }
            catch (Exception ex)
            {
                _lastScanResult =
                    null;

                _lastAvailableUpdates =
                    Array.Empty<
                        WindowsUpdateAvailableInfo>();

                _lastAvailableUpdateCount =
                    0;

                AvailableUpdates.Clear();

                OnPropertyChanged(
                    nameof(CanInstallUpdates));

                CommandManager
                    .InvalidateRequerySuggested();

                _lastErrorMessage =
                    ex.Message;

                ApplyErrorState();
            }
            finally
            {
                IsScanning = false;
            }
        }

        private async Task ConfirmInstallUpdatesAsync()
        {
            if (!CanInstallUpdates)
            {
                return;
            }

            bool confirmed =
                NativeConfirmationDialog.Ask(
                    Application.Current.MainWindow,
                    LocalizationHelper.Get(
                        "WindowsUpdateInstallConfirmationTitle"),
                    LocalizationHelper.Get(
                        "WindowsUpdateInstallConfirmationMessage"),
                    LocalizationHelper.Get(
                        "WindowsUpdateInstallConfirmYes"),
                    LocalizationHelper.Get(
                        "WindowsUpdateInstallConfirmNo"));

            if (!confirmed)
            {
                return;
            }

            try
            {
                string workerPath =
                    Path.Combine(
                        AppContext.BaseDirectory,
                        "WinBoost.UpdateWorker.exe");

                if (!File.Exists(workerPath))
                {
                    MessageBox.Show(
                        $"Update worker not found:\n\n{workerPath}",
                        "WinBoost Update Worker",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                InstallationState =
                    "Starting";

                InstallationPercent =
                    0;

                InstallationMessage =
                    string.Empty;

                CurrentUpdateTitle =
                    string.Empty;

                RebootRequired =
                    false;

                IsInstallingUpdates =
                    true;

                var startInfo =
                    new ProcessStartInfo
                    {
                        FileName =
                            workerPath,

                        UseShellExecute =
                            true,

                        Verb =
                            "runas"
                    };

                Process? workerProcess =
                    Process.Start(
                        startInfo);

                if (workerProcess == null)
                {
                    throw new InvalidOperationException(
                        "WinBoost Update Worker could not be started.");
                }

                await MonitorUpdateWorkerAsync(
                    workerProcess);
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                IsInstallingUpdates =
                    false;

                if (ex.NativeErrorCode == 1223)
                {
                    return;
                }

                MessageBox.Show(
                    ex.Message,
                    "WinBoost Update Worker",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                IsInstallingUpdates =
                    false;

                MessageBox.Show(
                    ex.Message,
                    "WinBoost Update Worker",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }


        private async Task MonitorUpdateWorkerAsync(
    Process workerProcess)
        {
            try
            {
                while (!workerProcess.HasExited)
                {
                    WindowsUpdateWorkerStatus? status =
                        await _workerStatusReader
                            .ReadAsync();

                    if (status != null)
                    {
                        ApplyWorkerStatus(
                            status);
                    }

                    await Task.Delay(
                        TimeSpan.FromSeconds(1));
                }

                WindowsUpdateWorkerStatus? finalStatus =
                    await _workerStatusReader
                        .ReadAsync();

                if (finalStatus != null)
                {
                    ApplyWorkerStatus(
                        finalStatus);
                }

                if (finalStatus == null)
                {
                    InstallationState =
                        "Completed";

                    InstallationMessage =
                        "Windows Update Worker finished.";

                    await Task.Delay(
                        TimeSpan.FromSeconds(2));

                    IsInstallingUpdates =
                        false;

                    return;
                }

                if (!finalStatus.IsCompleted)
                {
                    InstallationState =
                        "Completed";

                    InstallationMessage =
                        "Windows Update Worker finished.";

                    await Task.Delay(
                        TimeSpan.FromSeconds(2));

                    IsInstallingUpdates =
                        false;

                    return;
                }

                if (finalStatus.IsSuccessful)
                {
                    InstallationState =
                        "Completed";

                    InstallationPercent =
                        100;

                    RebootRequired =
                        finalStatus.RebootRequired;

                    if (string.IsNullOrWhiteSpace(
                        InstallationMessage))
                    {
                        InstallationMessage =
                            finalStatus.RebootRequired
                                ? "Windows updates were installed. A restart is required."
                                : "Windows updates were installed successfully.";
                    }

                    /*
                     * Păstrăm panoul vizibil puțin timp
                     * pentru ca utilizatorul să poată vedea
                     * rezultatul final și 100%.
                     */
                    await Task.Delay(
                        TimeSpan.FromSeconds(2));
                }
                else
                {
                    InstallationState =
                        "Error";

                    if (!string.IsNullOrWhiteSpace(
                        finalStatus.ErrorMessage))
                    {
                        InstallationMessage =
                            finalStatus.ErrorMessage;
                    }

                    /*
                     * Eroarea rămâne puțin mai mult
                     * pentru a putea fi citită.
                     */
                    await Task.Delay(
                        TimeSpan.FromSeconds(3));
                }

                IsInstallingUpdates =
                    false;
            }
            catch (Exception ex)
            {
                InstallationState =
                    "Error";

                InstallationMessage =
                    ex.Message;

                await Task.Delay(
                    TimeSpan.FromSeconds(3));

                IsInstallingUpdates =
                    false;
            }
        }

        private void ApplyWorkerStatus(
            WindowsUpdateWorkerStatus status)
        {
            InstallationState =
                status.State;

            InstallationPercent =
                status.Percent;

            InstallationMessage =
                status.Message;

            CurrentUpdateTitle =
                status.CurrentUpdateTitle;

            RebootRequired =
                status.RebootRequired;

            /*
             * Nu ascundem panoul aici când Worker-ul
             * termină. MonitorUpdateWorkerAsync îl va
             * păstra vizibil suficient pentru afișarea
             * rezultatului final și a valorii 100%.
             */
        }

        private void RefreshAvailableUpdates()
        {
            AvailableUpdates.Clear();

            foreach (
                WindowsUpdateAvailableInfo update
                in _lastAvailableUpdates)
            {
                AvailableUpdates.Add(
                    new WindowsUpdateAvailableDisplayItem
                    {
                        Title =
                            update.Title,

                        Description =
                            update.Description,

                        IsDownloaded =
                            GetLocalizedBoolean(
                                update.IsDownloaded),

                        RebootRequired =
                            GetLocalizedBoolean(
                                update.RebootRequired)
                    });
            }
        }

        private static string GetLocalizedBoolean(
            bool value)
        {
            return LocalizationHelper.Get(
                value
                    ? "WindowsUpdateYes"
                    : "WindowsUpdateNo");
        }

        private void ApplyScanResult(
            WindowsUpdateScanResult result,
            int availableUpdateCount)
        {
            string availableUpdatesText =
                LocalizationHelper.Format(
                    "WindowsUpdateAvailableCountFormat",
                    availableUpdateCount);

            if (result.DisabledServices.Count > 0)
            {
                ScanState =
                    "Warning";

                ScanBadgeText =
                    LocalizationHelper.Get(
                        "WindowsUpdateBadgeWarning");

                ScanStatus =
                    LocalizationHelper.Format(
                        "WindowsUpdateDisabledServicesFormat",
                        result.CheckedServices,
                        string.Join(
                            ", ",
                            result.DisabledServices))
                    + " "
                    + availableUpdatesText;

                return;
            }

            if (result.StoppedServices.Count > 0)
            {
                ScanState =
                    "Warning";

                ScanBadgeText =
                    LocalizationHelper.Get(
                        "WindowsUpdateBadgeWarning");

                ScanStatus =
                    LocalizationHelper.Format(
                        "WindowsUpdateStoppedServicesFormat",
                        result.CheckedServices,
                        result.RunningServices,
                        string.Join(
                            ", ",
                            result.StoppedServices))
                    + " "
                    + availableUpdatesText;

                return;
            }

            if (availableUpdateCount > 0)
            {
                ScanState =
                    "UpdatesAvailable";

                ScanBadgeText =
                    LocalizationHelper.Get(
                        "WindowsUpdateBadgeUpdatesAvailable");

                ScanStatus =
                    LocalizationHelper.Format(
                        "WindowsUpdateAllServicesRunningFormat",
                        result.CheckedServices)
                    + " "
                    + availableUpdatesText;

                return;
            }

            ScanState =
                "Checked";

            ScanBadgeText =
                LocalizationHelper.Get(
                    "WindowsUpdateBadgeChecked");

            ScanStatus =
                LocalizationHelper.Format(
                    "WindowsUpdateAllServicesRunningFormat",
                    result.CheckedServices)
                + " "
                + availableUpdatesText;
        }

        private void ApplyErrorState()
        {
            ScanState =
                "Error";

            ScanBadgeText =
                LocalizationHelper.Get(
                    "WindowsUpdateBadgeError");

            ScanStatus =
                LocalizationHelper.Format(
                    "WindowsUpdateScanFailedFormat",
                    _lastErrorMessage);
        }

        private void ApplyInitialUiState()
        {
            ScanState =
                "NotChecked";

            ScanStatus =
                LocalizationHelper.Get(
                    "WindowsUpdateScanPrompt");

            ScanBadgeText =
                LocalizationHelper.Get(
                    "WindowsUpdateBadgeNotChecked");

            OnPropertyChanged(
                nameof(ScanButtonText));

            OnPropertyChanged(
                nameof(InstallButtonText));

            OnPropertyChanged(
                nameof(CanInstallUpdates));
        }

        private void RefreshLocalizedUi()
        {
            OnPropertyChanged(
                nameof(ScanButtonText));

            OnPropertyChanged(
                nameof(InstallButtonText));

            RefreshAvailableUpdates();

            if (IsScanning)
            {
                ScanState =
                    "Checking";

                ScanBadgeText =
                    LocalizationHelper.Get(
                        "WindowsUpdateBadgeChecking");

                ScanStatus =
                    LocalizationHelper.Get(
                        "WindowsUpdateScanningStatus");

                return;
            }

            if (_lastScanResult != null)
            {
                ApplyScanResult(
                    _lastScanResult,
                    _lastAvailableUpdateCount);

                return;
            }

            if (ScanState == "Error")
            {
                ApplyErrorState();

                return;
            }

            ApplyInitialUiState();
        }

        private void LanguageManager_LanguageChanged(
            object? sender,
            EventArgs e)
        {
            RefreshLocalizedUi();
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