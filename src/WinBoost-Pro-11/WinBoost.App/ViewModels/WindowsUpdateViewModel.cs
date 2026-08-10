using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using WinBoost.App.Commands;
using WinBoost.App.Localization;
using WinBoost.App.Services.WindowsUpdate;

namespace WinBoost.App.ViewModels
{
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
                    WindowsUpdateAvailableInfo>();

            ScanUpdatesCommand =
                new RelayCommand(
                    async _ =>
                        await ScanUpdatesAsync(),
                    _ =>
                        !IsScanning);

            ApplyInitialUiState();

            LanguageManager.Instance.LanguageChanged +=
                LanguageManager_LanguageChanged;
        }

        public ICommand ScanUpdatesCommand
        {
            get;
        }

        public ObservableCollection<
            WindowsUpdateAvailableInfo>
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

                CommandManager
                    .InvalidateRequerySuggested();
            }
        }

        public string ScanButtonText =>
            IsScanning
                ? LocalizationHelper.Get(
                    "WindowsUpdateScanningButton")
                : LocalizationHelper.Get(
                    "WindowsUpdateScanButton");

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
                WindowsUpdateScanResult result =
                    await _windowsUpdateScanner
                        .ScanAsync();

                WindowsUpdateAvailableResult availableResult =
                    await _windowsUpdateAvailableScanner
                        .ScanAsync();

                AvailableUpdates.Clear();

                foreach (
                    WindowsUpdateAvailableInfo update
                    in availableResult.Updates)
                {
                    AvailableUpdates.Add(
                        update);
                }

                _lastScanResult =
                    result;

                _lastAvailableUpdateCount =
                    availableResult.UpdateCount;

                ApplyScanResult(
                    result,
                    availableResult.UpdateCount);
            }
            catch (Exception ex)
            {
                _lastScanResult =
                    null;

                _lastAvailableUpdateCount =
                    0;

                AvailableUpdates.Clear();

                _lastErrorMessage =
                    ex.Message;

                ApplyErrorState();
            }
            finally
            {
                IsScanning = false;
            }
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
        }

        private void RefreshLocalizedUi()
        {
            OnPropertyChanged(
                nameof(ScanButtonText));

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