using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using WinBoost.App.Models;
using WinBoost.App.Commands;
using WinBoost.App.Helpers;
using WinBoost.App.Localization;
using WinBoost.App.Services.Alerts;
using WinBoost.App.Services.History;
using WinBoost.App.Services.Licensing;
using System.Text.RegularExpressions;
using System.Net.Mail;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;


namespace WinBoost.App.ViewModels
{
    public sealed class SettingsViewModel :
        INotifyPropertyChanged
    {
        private PerformanceAlertSettingsService
            _settingsService;

        private PerformanceAlertSettings
            _settings;

        private readonly PerformanceHistorySettingsService
           _historySettingsService;

        private PerformanceHistorySettings
                _historySettings;

        private readonly PerformanceHistoryRecorder
             _historyRecorder;

        private readonly LicenseService
             _licenseService;

        private readonly LicensePurchaseService
             _licensePurchaseService;

        private readonly LicenseOffersService
              _licenseOffersService;

        private string _licenseKeyInput =
             string.Empty;

        private string _customerEmailInput =
                string.Empty;

        private LicenseOfferDisplayItem?
            _selectedLicenseOffer;

        private bool
            _isLoadingLicenseOffers;

        public bool CanActivateLicense =>
            !string.IsNullOrWhiteSpace(
        LicenseKeyInput);



        public SettingsViewModel()
        {
            _settingsService =
                new PerformanceAlertSettingsService();

            _settings =
                _settingsService.Load();

            _historySettingsService =
                 new PerformanceHistorySettingsService();

            _historySettings =
                _historySettingsService.Load();

            _historyRecorder =
                   new PerformanceHistoryRecorder();

            _licenseService =
                   LicenseService.Instance;

            _licensePurchaseService =
                new LicensePurchaseService();

            _licenseOffersService =
                new LicenseOffersService();

            AvailableLicenseOffers =
                new ObservableCollection<
                    LicenseOfferDisplayItem>();

            _licenseService.LicenseChanged +=
                OnLicenseChanged;

            WeakEventManager<LanguageManager, EventArgs>
                  .AddHandler(
                     LanguageManager.Instance,
                     nameof(LanguageManager.LanguageChanged),
                      OnLanguageChanged);

            ClearHistoryCommand =
                new RelayCommand(
                    async _ => await ClearHistoryAsync());

            ResetSettingsCommand =
                   new RelayCommand(
                      _ => ResetSettings());

            ActivateLicenseCommand =
                     new RelayCommand(
                     _ => ActivateLicense());

            PurchaseLicenseCommand =
                   new RelayCommand(
                        async _ =>
                         await PurchaseLicenseAsync());

            RestartWithDifferentPrivilegesCommand =
                new RelayCommand(
                    _ => RestartWithDifferentPrivileges());

            _ = LoadLicenseOffersAsync();
        }

        public ICommand RestartWithDifferentPrivilegesCommand
        {
            get;
        }

        public bool IsRunningAsAdministrator =>
            ApplicationElevationHelper
                .IsRunningAsAdministrator();

        public string CurrentPrivilegeModeText =>
            IsRunningAsAdministrator
                ? LocalizationHelper.Get(
                    "SettingsPrivilegeModeAdministrator")
                : LocalizationHelper.Get(
                    "SettingsPrivilegeModeNormal");

        public string RestartPrivilegeButtonText =>
            IsRunningAsAdministrator
                ? LocalizationHelper.Get(
                    "SettingsRestartNormally")
                : LocalizationHelper.Get(
                    "SettingsRestartAsAdministrator");

        public string LicenseStatusText =>
          LicenseDisplayHelper.GetStatusText(
        _licenseService.Status);


        public string LicenseKeyInput
        {
            get => _licenseKeyInput;

            set
            {
                if (_licenseKeyInput == value)
                {
                    return;
                }

                _licenseKeyInput =
                    value;

                OnPropertyChanged();

                OnPropertyChanged(
                nameof(CanActivateLicense));
            }
        }

        public string CustomerEmailInput
        {
            get => _customerEmailInput;

            set
            {
                if (_customerEmailInput == value)
                {
                    return;
                }

                _customerEmailInput =
                    value;

                OnPropertyChanged();

                OnPropertyChanged(
                    nameof(CanStartLicensePurchase));
            }
        }

        public ObservableCollection<
            LicenseOfferDisplayItem>
            AvailableLicenseOffers
        {
            get;
        }

        public LicenseOfferDisplayItem?
            SelectedLicenseOffer
        {
            get => _selectedLicenseOffer;

            set
            {
                if (_selectedLicenseOffer == value)
                {
                    return;
                }

                _selectedLicenseOffer =
                    value;

                OnPropertyChanged();

                OnPropertyChanged(
                    nameof(CanStartLicensePurchase));
            }
        }

        public bool IsLoadingLicenseOffers
        {
            get => _isLoadingLicenseOffers;

            private set
            {
                if (_isLoadingLicenseOffers == value)
                {
                    return;
                }

                _isLoadingLicenseOffers =
                    value;

                OnPropertyChanged();

                OnPropertyChanged(
                    nameof(LicenseOffersStatusText));
            }
        }

        public bool HasAvailableLicenseOffers =>
            AvailableLicenseOffers.Count > 0;

        public string LicenseOffersStatusText
        {
            get
            {
                if (IsLoadingLicenseOffers)
                {
                    return LocalizationHelper.Get(
                        "SettingsLicenseOffersLoading");
                }

                if (HasAvailableLicenseOffers)
                {
                    return string.Empty;
                }

                return LocalizationHelper.Get(
                    "SettingsLicenseOffersUnavailable");
            }
        }

        public bool CanStartLicensePurchase
        {
            get
            {
                string email =
                    CustomerEmailInput
                        .Trim();

                bool validEmail =
                    MailAddress.TryCreate(
                        email,
                        out MailAddress? address) &&
                    string.Equals(
                        address.Address,
                        email,
                        StringComparison.OrdinalIgnoreCase);

                return validEmail &&
                       SelectedLicenseOffer != null;
            }
        }

        public ICommand ClearHistoryCommand
        {
            get;
        }

        public ICommand PurchaseLicenseCommand
        {
            get;
        }

        public bool AlertsEnabled
        {
            get => _settings.AlertsEnabled;

            set
            {
                if (_settings.AlertsEnabled == value)
                {
                    return;
                }

                _settings.AlertsEnabled = value;

                _settingsService.Save(_settings);

                OnPropertyChanged();
            }
        }

        public ICommand ResetSettingsCommand
        {
            get;
        }

        public ICommand ActivateLicenseCommand
        {
            get;
        }

        public double CpuWarningThreshold
        {
            get => _settings.CpuWarningThreshold;

            set
            {
                double normalizedValue =
                    Math.Clamp(value, 1.0, 100.0);

                normalizedValue =
                    Math.Min(
                        normalizedValue,
                        _settings.CpuCriticalThreshold);

                if (Math.Abs(
                        _settings.CpuWarningThreshold -
                        normalizedValue) < 0.01)
                {
                    return;
                }

                _settings.CpuWarningThreshold =
                    normalizedValue;

                _settingsService.Save(_settings);

                OnPropertyChanged();
            }
        }

        public double CpuCriticalThreshold
        {
            get => _settings.CpuCriticalThreshold;

            set
            {
                double normalizedValue =
                    Math.Clamp(value, 1.0, 100.0);

                normalizedValue =
                    Math.Max(
                        normalizedValue,
                        _settings.CpuWarningThreshold);

                if (Math.Abs(
                        _settings.CpuCriticalThreshold -
                        normalizedValue) < 0.01)
                {
                    return;
                }

                _settings.CpuCriticalThreshold =
                    normalizedValue;

                _settingsService.Save(_settings);

                OnPropertyChanged();
            }
        }

        public double RamWarningThreshold
        {
            get => _settings.RamWarningThreshold;

            set
            {
                double normalizedValue =
                    Math.Clamp(value, 1.0, 100.0);

                normalizedValue =
                    Math.Min(
                        normalizedValue,
                        _settings.RamCriticalThreshold);

                if (Math.Abs(
                        _settings.RamWarningThreshold -
                        normalizedValue) < 0.01)
                {
                    return;
                }

                _settings.RamWarningThreshold =
                    normalizedValue;

                _settingsService.Save(_settings);

                OnPropertyChanged();
            }
        }

        public double RamCriticalThreshold
        {
            get => _settings.RamCriticalThreshold;

            set
            {
                double normalizedValue =
                    Math.Clamp(value, 1.0, 100.0);

                normalizedValue =
                    Math.Max(
                        normalizedValue,
                        _settings.RamWarningThreshold);

                if (Math.Abs(
                        _settings.RamCriticalThreshold -
                        normalizedValue) < 0.01)
                {
                    return;
                }

                _settings.RamCriticalThreshold =
                    normalizedValue;

                _settingsService.Save(_settings);

                OnPropertyChanged();
            }
        }

        public double DiskWarningThreshold
        {
            get => _settings.DiskWarningThreshold;

            set
            {
                double normalizedValue =
                    Math.Clamp(value, 1.0, 100.0);

                normalizedValue =
                    Math.Min(
                        normalizedValue,
                        _settings.DiskCriticalThreshold);

                if (Math.Abs(
                        _settings.DiskWarningThreshold -
                        normalizedValue) < 0.01)
                {
                    return;
                }

                _settings.DiskWarningThreshold =
                    normalizedValue;

                _settingsService.Save(_settings);

                OnPropertyChanged();
            }
        }

        public double DiskCriticalThreshold
        {
            get => _settings.DiskCriticalThreshold;

            set
            {
                double normalizedValue =
                    Math.Clamp(value, 1.0, 100.0);

                normalizedValue =
                    Math.Max(
                        normalizedValue,
                        _settings.DiskWarningThreshold);

                if (Math.Abs(
                        _settings.DiskCriticalThreshold -
                        normalizedValue) < 0.01)
                {
                    return;
                }

                _settings.DiskCriticalThreshold =
                    normalizedValue;

                _settingsService.Save(_settings);

                OnPropertyChanged();
            }
        }

        public double CpuTemperatureWarningThreshold
        {
            get => _settings.CpuTemperatureWarningThreshold;

            set
            {
                double normalizedValue =
                    Math.Clamp(value, 30.0, 110.0);

                normalizedValue =
                    Math.Min(
                        normalizedValue,
                        _settings
                            .CpuTemperatureCriticalThreshold);

                if (Math.Abs(
                        _settings
                            .CpuTemperatureWarningThreshold -
                        normalizedValue) < 0.01)
                {
                    return;
                }

                _settings.CpuTemperatureWarningThreshold =
                    normalizedValue;

                _settingsService.Save(_settings);

                OnPropertyChanged();
            }
        }

        public double CpuTemperatureCriticalThreshold
        {
            get => _settings.CpuTemperatureCriticalThreshold;

            set
            {
                double normalizedValue =
                    Math.Clamp(value, 30.0, 110.0);

                normalizedValue =
                    Math.Max(
                        normalizedValue,
                        _settings
                            .CpuTemperatureWarningThreshold);

                if (Math.Abs(
                        _settings
                            .CpuTemperatureCriticalThreshold -
                        normalizedValue) < 0.01)
                {
                    return;
                }

                _settings.CpuTemperatureCriticalThreshold =
                    normalizedValue;

                _settingsService.Save(_settings);

                OnPropertyChanged();
            }
        }

        public int SustainedDurationSeconds
        {
            get => _settings.SustainedDurationSeconds;

            set
            {
                int normalizedValue =
                    Math.Clamp(value, 5, 300);

                if (_settings.SustainedDurationSeconds ==
                    normalizedValue)
                {
                    return;
                }

                _settings.SustainedDurationSeconds =
                    normalizedValue;

                _settingsService.Save(_settings);

                OnPropertyChanged();
            }
        }

        public bool EnableSoundForCriticalAlerts
        {
            get => _settings.EnableSoundForCriticalAlerts;

            set
            {
                if (_settings.EnableSoundForCriticalAlerts ==
                    value)
                {
                    return;
                }

                _settings.EnableSoundForCriticalAlerts =
                    value;

                _settingsService.Save(_settings);

                OnPropertyChanged();
            }
        }

        public int CriticalAlertRepeatIntervalMinutes
        {
            get => _settings
                .CriticalAlertRepeatIntervalMinutes;

            set
            {
                int normalizedValue =
                    NormalizeRepeatInterval(value);

                if (_settings
                    .CriticalAlertRepeatIntervalMinutes ==
                    normalizedValue)
                {
                    return;
                }

                _settings
                    .CriticalAlertRepeatIntervalMinutes =
                    normalizedValue;

                _settingsService.Save(_settings);

                OnPropertyChanged();
            }
        }

        private static int NormalizeRepeatInterval(
            int value)
        {
            return value switch
            {
                <= 0 => 0,
                <= 5 => 5,
                <= 15 => 15,
                _ => 30
            };
        }

        public bool CpuAlertsEnabled
        {
            get => _settings.CpuAlertsEnabled;

            set
            {
                if (_settings.CpuAlertsEnabled == value)
                {
                    return;
                }

                _settings.CpuAlertsEnabled = value;

                _settingsService.Save(_settings);

                OnPropertyChanged();
            }
        }

        public bool RamAlertsEnabled
        {
            get => _settings.RamAlertsEnabled;

            set
            {
                if (_settings.RamAlertsEnabled == value)
                {
                    return;
                }

                _settings.RamAlertsEnabled = value;

                _settingsService.Save(_settings);

                OnPropertyChanged();
            }
        }

        public bool DiskAlertsEnabled
        {
            get => _settings.DiskAlertsEnabled;

            set
            {
                if (_settings.DiskAlertsEnabled == value)
                {
                    return;
                }

                _settings.DiskAlertsEnabled = value;

                _settingsService.Save(_settings);

                OnPropertyChanged();
            }
        }

        public bool CpuTemperatureAlertsEnabled
        {
            get => _settings.CpuTemperatureAlertsEnabled;

            set
            {
                if (_settings.CpuTemperatureAlertsEnabled ==
                    value)
                {
                    return;
                }

                _settings.CpuTemperatureAlertsEnabled =
                    value;

                _settingsService.Save(_settings);

                OnPropertyChanged();
            }
        }

        public int HistoryRetentionDays
        {
            get => _historySettings.RetentionDays;

            set
            {
                int normalizedValue =
                    NormalizeHistoryRetentionDays(value);

                if (_historySettings.RetentionDays ==
                    normalizedValue)
                {
                    return;
                }

                _historySettings.RetentionDays =
                    normalizedValue;

                _historySettingsService.Save(
                    _historySettings);

                OnPropertyChanged();
            }
        }

        private static int NormalizeHistoryRetentionDays(
            int value)
        {
            return value switch
            {
                <= 7 => 7,
                <= 14 => 14,
                <= 30 => 30,
                _ => 90
            };
        }

        public event PropertyChangedEventHandler?
            PropertyChanged;
        private async Task ClearHistoryAsync()
        {
            bool confirmed =
             NativeConfirmationDialog.Ask(
                      Application.Current.MainWindow,
                       LocalizationHelper.Get(
                       "SettingsClearHistoryConfirmTitle"),
                       LocalizationHelper.Get(
                        "SettingsClearHistoryConfirmMessage"),
                        LocalizationHelper.Get("CommonYes"),
                       LocalizationHelper.Get("CommonNo"));

            if (!confirmed)
            {
                return;
            }

            try
            {
                await _historyRecorder.ClearHistoryAsync();

                MessageBox.Show(
                    Application.Current.MainWindow,
                    LocalizationHelper.Get(
                        "SettingsClearHistorySuccessMessage"),
                    LocalizationHelper.Get(
                        "SettingsClearHistorySuccessTitle"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (System.Exception exception)
            {
                MessageBox.Show(
                    Application.Current.MainWindow,
                    exception.Message,
                    LocalizationHelper.Get("CommonError"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ResetSettings()
        {
            bool confirmed =
                NativeConfirmationDialog.Ask(
                    Application.Current.MainWindow,
                    LocalizationHelper.Get(
                        "SettingsResetConfirmTitle"),
                    LocalizationHelper.Get(
                        "SettingsResetConfirmMessage"),
                    LocalizationHelper.Get("CommonYes"),
                    LocalizationHelper.Get("CommonNo"));

            if (!confirmed)
            {
                return;
            }

            _settings =
                new PerformanceAlertSettings();

            _historySettings =
                new PerformanceHistorySettings();

            _settingsService.Save(_settings);

            _historySettingsService.Save(
                _historySettings);

            OnPropertyChanged(string.Empty);

            MessageBox.Show(
                Application.Current.MainWindow,
                LocalizationHelper.Get(
                    "SettingsResetSuccessMessage"),
                LocalizationHelper.Get(
                    "SettingsResetSuccessTitle"),
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void RestartWithDifferentPrivileges()
        {
            if (IsRunningAsAdministrator)
            {
                ApplicationElevationHelper
                    .RestartNormally();

                return;
            }

            ApplicationElevationHelper
                .RestartAsAdministrator();
        }

        private void ActivateLicense()
        {
            string licenseKey =
                LicenseKeyInput
                    .Trim()
                    .ToUpperInvariant();

            bool isValidFormat =
                Regex.IsMatch(
                    licenseKey,
                    @"^WB11-[A-Z0-9]{4}-[A-Z0-9]{4}-[A-Z0-9]{4}-[A-Z0-9]{4}$");

            if (!isValidFormat)
            {
                MessageBox.Show(
                    Application.Current.MainWindow,
                    LocalizationHelper.Get(
                        "SettingsLicenseInvalidFormatMessage"),
                    LocalizationHelper.Get(
                        "SettingsLicenseInvalidFormatTitle"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            MessageBox.Show(
                Application.Current.MainWindow,
                LocalizationHelper.Get(
                    "SettingsLicenseValidFormatMessage"),
                LocalizationHelper.Get(
                    "SettingsLicenseValidFormatTitle"),
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private async Task LoadLicenseOffersAsync()
        {
            IsLoadingLicenseOffers =
                true;

            try
            {
                IReadOnlyList<LicenseOfferDisplayItem> offers =
                    await _licenseOffersService
                        .GetDisplayOffersAsync();

                AvailableLicenseOffers.Clear();

                foreach (LicenseOfferDisplayItem offer in offers)
                {
                    AvailableLicenseOffers.Add(
                        offer);
                }

                SelectedLicenseOffer =
                    AvailableLicenseOffers
                        .FirstOrDefault();

                OnPropertyChanged(
                    nameof(HasAvailableLicenseOffers));

                OnPropertyChanged(
                    nameof(CanStartLicensePurchase));
            }
            finally
            {
                IsLoadingLicenseOffers =
                    false;

                OnPropertyChanged(
                    nameof(LicenseOffersStatusText));
            }
        }

        private async Task PurchaseLicenseAsync()
        {
            string email =
                CustomerEmailInput
                    .Trim();

            if (!CanStartLicensePurchase)
            {
                NativeMessageDialog.Show(
                    Application.Current.MainWindow,
                    LocalizationHelper.Get(
                        "SettingsLicenseInvalidEmailTitle"),
                    LocalizationHelper.Get(
                        "SettingsLicenseInvalidEmailMessage"),
                    LocalizationHelper.Get(
                        "CommonClose"));

                return;
            }

            PurchaseSessionResponse response =
                await _licensePurchaseService
                    .StartPurchaseAsync(
                        email,
                        SelectedLicenseOffer!.Plan);

            if (response.Success)
            {
                return;
            }

            string messageKey =
                response.ErrorCode switch
                {
                    "SERVER_NOT_CONFIGURED" =>
                        "SettingsLicenseServerNotConfigured",

                    "NETWORK_ERROR" =>
                        "SettingsLicenseNetworkError",

                    "REQUEST_TIMEOUT" =>
                        "SettingsLicenseTimeoutError",

                    "INVALID_CHECKOUT_URL" =>
                        "SettingsLicenseCheckoutError",

                    _ =>
                        "SettingsLicensePurchaseError"
                };

            NativeMessageDialog.Show(
                Application.Current.MainWindow,
                LocalizationHelper.Get(
                    "SettingsLicensePurchaseErrorTitle"),
                LocalizationHelper.Get(
                    messageKey),
                LocalizationHelper.Get(
                    "CommonClose"));
        }

        private void OnLicenseChanged(
                object? sender,
                 EventArgs e)
        {
            OnPropertyChanged(
                nameof(LicenseStatusText));
        }

        private void OnLanguageChanged(
            object? sender,
            EventArgs e)
        {
            OnPropertyChanged(
                nameof(LicenseStatusText));

            OnPropertyChanged(
                nameof(LicenseOffersStatusText));

            _ = LoadLicenseOffersAsync();
        }

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