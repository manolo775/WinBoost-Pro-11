using Microsoft.Win32;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using WinBoost.App.Localization;

namespace WinBoost.App.Models
{
    public sealed class StartupAppInfo :
        INotifyPropertyChanged
    {
        private bool _isEnabled;

        private string _source =
            string.Empty;

        private string _startupImpact =
            string.Empty;

        private string _startupType =
            string.Empty;

        public string Name
        {
            get;
            set;
        } =
            string.Empty;

        public string Command
        {
            get;
            set;
        } =
            string.Empty;

        public string SourceResourceKey
        {
            get;
            set;
        } =
            string.Empty;

        public string Source
        {
            get =>
                !string.IsNullOrWhiteSpace(
                    SourceResourceKey)
                    ? LocalizationHelper.Get(
                        SourceResourceKey)
                    : _source;

            set =>
                _source =
                    value ?? string.Empty;
        }

        public RegistryHive RegistryHive
        {
            get;
            set;
        }

        public string RegistryPath
        {
            get;
            set;
        } =
            string.Empty;

        public string RegistryValueName
        {
            get;
            set;
        } =
            string.Empty;

        public string ExecutablePath
        {
            get;
            set;
        } =
            string.Empty;

        public string Arguments
        {
            get;
            set;
        } =
            string.Empty;

        public string Publisher
        {
            get;
            set;
        } =
            string.Empty;

        public string Description
        {
            get;
            set;
        } =
            string.Empty;

        public string FileVersion
        {
            get;
            set;
        } =
            string.Empty;

        public string StartupImpactResourceKey
        {
            get;
            set;
        } =
            string.Empty;

        public string StartupImpact
        {
            get =>
                !string.IsNullOrWhiteSpace(
                    StartupImpactResourceKey)
                    ? LocalizationHelper.Get(
                        StartupImpactResourceKey)
                    : _startupImpact;

            set =>
                _startupImpact =
                    value ?? string.Empty;
        }

        public string StartupTypeResourceKey
        {
            get;
            set;
        } =
            string.Empty;

        public string StartupType
        {
            get =>
                !string.IsNullOrWhiteSpace(
                    StartupTypeResourceKey)
                    ? LocalizationHelper.Get(
                        StartupTypeResourceKey)
                    : _startupType;

            set =>
                _startupType =
                    value ?? string.Empty;
        }

        public bool RequiresAdministrator
        {
            get;
            set;
        }

        public bool FileExists =>
            !string.IsNullOrWhiteSpace(
                ExecutablePath) &&
            File.Exists(
                ExecutablePath);

        public bool IsRegistryEntry =>
            !string.IsNullOrWhiteSpace(
                RegistryPath) &&
            !string.IsNullOrWhiteSpace(
                RegistryValueName);

        public bool IsEnabled
        {
            get => _isEnabled;

            set
            {
                if (_isEnabled == value)
                {
                    return;
                }

                _isEnabled =
                    value;

                OnPropertyChanged();

                OnPropertyChanged(
                    nameof(Status));

                OnPropertyChanged(
                    nameof(ActionText));
            }
        }

        public string Status =>
            IsEnabled
                ? LocalizationHelper.Get(
                    "StartupStatusEnabled")
                : LocalizationHelper.Get(
                    "StartupStatusDisabled");

        public string ActionText =>
            IsEnabled
                ? LocalizationHelper.Get(
                    "StartupActionDisable")
                : LocalizationHelper.Get(
                    "StartupActionEnable");

        public string DisplayPath =>
            string.IsNullOrWhiteSpace(
                ExecutablePath)
                ? Command
                : ExecutablePath;

        public event PropertyChangedEventHandler?
            PropertyChanged;

        public void RefreshLocalizedProperties()
        {
            OnPropertyChanged(
                nameof(Source));

            OnPropertyChanged(
                nameof(Status));

            OnPropertyChanged(
                nameof(ActionText));

            OnPropertyChanged(
                nameof(StartupImpact));

            OnPropertyChanged(
                nameof(StartupType));
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