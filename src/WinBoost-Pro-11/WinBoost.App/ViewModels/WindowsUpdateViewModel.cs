using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
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
                    _ =>
                        ConfirmInstallUpdates(),
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

        public bool CanInstallUpdates =>
            !IsScanning &&
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

        private void ConfirmInstallUpdates()
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

                Process.Start(
                    startInfo);
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                if (ex.NativeErrorCode == 1223)
                {
                    // Utilizatorul a anulat fereastra UAC.
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
                MessageBox.Show(
                    ex.Message,
                    "WinBoost Update Worker",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
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