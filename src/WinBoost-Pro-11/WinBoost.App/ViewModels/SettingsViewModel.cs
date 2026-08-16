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

            ClearHistoryCommand =
                new RelayCommand(
                    async _ => await ClearHistoryAsync());

            ResetSettingsCommand =
                   new RelayCommand(
                      _ => ResetSettings());

            RestartWithDifferentPrivilegesCommand =
                   new RelayCommand(
                     _ => RestartWithDifferentPrivileges());

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
        public ICommand ClearHistoryCommand
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