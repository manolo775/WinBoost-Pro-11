using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using WinBoost.App.Commands;
using WinBoost.App.Localization;
using WinBoost.App.Models;
using WinBoost.App.Services.Health;
using WinBoost.App.Services.Startup;

namespace WinBoost.App.ViewModels
{
    public sealed class StartupViewModel :
        INotifyPropertyChanged
    {
        private readonly StartupAppsScanner
            _startupAppsScanner;

        private readonly StartupAppsManager
            _startupAppsManager;

        private readonly SystemHealthStateService
            _healthStateService;

        private bool _isScanning;
        private bool _isChangingStartupState;

        private string _searchText =
            string.Empty;

        private int _selectedFilterIndex;

        private string _scanStatus =
            string.Empty;

        private string _scanBadgeText =
            string.Empty;

        private Brush _scanBadgeBrush =
            Brushes.LightGray;

        private StartupUiState _currentUiState =
            StartupUiState.NotScanned;

        private string _currentApplicationName =
            string.Empty;

        private string _currentErrorMessage =
            string.Empty;

        public StartupViewModel()
        {
            _startupAppsScanner =
                new StartupAppsScanner();

            _startupAppsManager =
                new StartupAppsManager();

            _healthStateService =
                SystemHealthStateService.Instance;

            StartupApplications =
                new ObservableCollection<
                    StartupAppInfo>();

            StartupFilters =
                new ObservableCollection<string>();

            StartupApplicationsView =
                CollectionViewSource.GetDefaultView(
                    StartupApplications);

            StartupApplicationsView.Filter =
                FilterStartupApplication;

            ScanStartupCommand =
                new RelayCommand(
                    async _ =>
                        await ScanStartupAsync(),
                    _ =>
                        !IsScanning &&
                        !IsChangingStartupState);

            ToggleStartupCommand =
                new RelayCommand(
                    async parameter =>
                        await ToggleStartupApplicationAsync(
                            parameter as StartupAppInfo),
                    parameter =>
                        parameter is StartupAppInfo &&
                        !IsScanning &&
                        !IsChangingStartupState);

            RefreshLocalizedFilters();

            ApplyUiState();

            LanguageManager.Instance.LanguageChanged +=
                LanguageManager_LanguageChanged;
        }

        public ObservableCollection<StartupAppInfo>
            StartupApplications
        {
            get;
        }

        public ICollectionView
            StartupApplicationsView
        {
            get;
        }

        public ObservableCollection<string>
            StartupFilters
        {
            get;
        }

        public ICommand ScanStartupCommand
        {
            get;
        }

        public ICommand ToggleStartupCommand
        {
            get;
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

                _searchText =
                    value ?? string.Empty;

                OnPropertyChanged();

                StartupApplicationsView.Refresh();
            }
        }

        public int SelectedFilterIndex
        {
            get => _selectedFilterIndex;

            set
            {
                int normalizedValue =
                    Math.Clamp(
                        value,
                        0,
                        2);

                if (_selectedFilterIndex ==
                    normalizedValue)
                {
                    return;
                }

                _selectedFilterIndex =
                    normalizedValue;

                OnPropertyChanged();

                StartupApplicationsView.Refresh();
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

        public Brush ScanBadgeBrush
        {
            get => _scanBadgeBrush;

            private set
            {
                if (_scanBadgeBrush == value)
                {
                    return;
                }

                _scanBadgeBrush =
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

        public bool IsChangingStartupState
        {
            get => _isChangingStartupState;

            private set
            {
                if (_isChangingStartupState == value)
                {
                    return;
                }

                _isChangingStartupState =
                    value;

                OnPropertyChanged();

                CommandManager
                    .InvalidateRequerySuggested();
            }
        }

        public string ScanButtonText =>
            IsScanning
                ? LocalizationHelper.Get(
                    "StartupScanningButton")
                : LocalizationHelper.Get(
                    "StartupScanButton");

        public int TotalApplications =>
            StartupApplications.Count;

        public int EnabledApplications =>
            StartupApplications.Count(
                application =>
                    application.IsEnabled);

        public int DisabledApplications =>
            StartupApplications.Count(
                application =>
                    !application.IsEnabled);

        private async Task ScanStartupAsync()
        {
            if (IsScanning ||
                IsChangingStartupState)
            {
                return;
            }

            IsScanning = true;

            SetUiState(
                StartupUiState.Scanning);

            try
            {
                var applications =
                    await _startupAppsScanner
                        .ScanAsync();

                StartupApplications.Clear();

                foreach (StartupAppInfo application
                         in applications)
                {
                    StartupApplications.Add(
                        application);
                }

                RefreshApplicationStatistics();

                UpdateStartupHealthScore();

                SetUiState(
                    StartupApplications.Count == 0
                        ? StartupUiState.ScanCompletedEmpty
                        : StartupUiState.ScanCompleted);
            }
            catch (Exception ex)
            {
                _currentErrorMessage =
                    ex.Message;

                SetUiState(
                    StartupUiState.ScanFailed);
            }
            finally
            {
                IsScanning = false;

                StartupApplicationsView.Refresh();

                CommandManager
                    .InvalidateRequerySuggested();
            }
        }

        private async Task ToggleStartupApplicationAsync(
            StartupAppInfo? application)
        {
            if (application == null ||
                IsScanning ||
                IsChangingStartupState)
            {
                return;
            }

            bool enableApplication =
                !application.IsEnabled;

            IsChangingStartupState = true;

            _currentApplicationName =
                application.Name;

            SetUiState(
                enableApplication
                    ? StartupUiState.EnablingApplication
                    : StartupUiState.DisablingApplication);

            try
            {
                await _startupAppsManager
                    .SetEnabledAsync(
                        application,
                        enableApplication);

                application.IsEnabled =
                    enableApplication;

                RefreshApplicationStatistics();

                UpdateStartupHealthScore();

                SetUiState(
                    enableApplication
                        ? StartupUiState.ApplicationEnabled
                        : StartupUiState.ApplicationDisabled);
            }
            catch (Exception ex)
            {
                _currentErrorMessage =
                    ex.Message;

                SetUiState(
                    StartupUiState.ChangeFailed);
            }
            finally
            {
                IsChangingStartupState = false;

                StartupApplicationsView.Refresh();

                CommandManager
                    .InvalidateRequerySuggested();
            }
        }

        private bool FilterStartupApplication(
            object item)
        {
            if (item is not StartupAppInfo application)
            {
                return false;
            }

            bool matchesSelectedFilter =
                SelectedFilterIndex switch
                {
                    1 =>
                        application.IsEnabled,

                    2 =>
                        !application.IsEnabled,

                    _ =>
                        true
                };

            if (!matchesSelectedFilter)
            {
                return false;
            }

            string searchText =
                SearchText.Trim();

            if (string.IsNullOrWhiteSpace(
                    searchText))
            {
                return true;
            }

            return ContainsSearchText(
                       application.Name,
                       searchText) ||

                   ContainsSearchText(
                       application.Publisher,
                       searchText) ||

                   ContainsSearchText(
                       application.Description,
                       searchText) ||

                   ContainsSearchText(
                       application.Source,
                       searchText) ||

                   ContainsSearchText(
                       application.Status,
                       searchText) ||

                   ContainsSearchText(
                       application.ExecutablePath,
                       searchText) ||

                   ContainsSearchText(
                       application.Command,
                       searchText);
        }

        private static bool ContainsSearchText(
            string? value,
            string searchText)
        {
            return !string.IsNullOrWhiteSpace(
                       value) &&
                   value.Contains(
                       searchText,
                       StringComparison
                           .CurrentCultureIgnoreCase);
        }

        private void RefreshApplicationStatistics()
        {
            OnPropertyChanged(
                nameof(TotalApplications));

            OnPropertyChanged(
                nameof(EnabledApplications));

            OnPropertyChanged(
                nameof(DisabledApplications));
        }

        private void UpdateStartupHealthScore()
        {
            int totalStartupApps =
                StartupApplications.Count;

            int enabledStartupApps =
                StartupApplications.Count(
                    application =>
                        application.IsEnabled);

            _healthStateService
                .UpdateStartupData(
                    totalStartupApps,
                    enabledStartupApps);

            int startupScore =
                enabledStartupApps switch
                {
                    >= 15 => 40,
                    >= 10 => 60,
                    >= 5 => 80,
                    _ => 100
                };

            WinBoostHealthScoreService
                .Instance
                .StartupScore =
                    startupScore;
        }

        private void RefreshLocalizedFilters()
        {
            int selectedIndex =
                SelectedFilterIndex;

            StartupFilters.Clear();

            StartupFilters.Add(
                LocalizationHelper.Get(
                    "StartupFilterAll"));

            StartupFilters.Add(
                LocalizationHelper.Get(
                    "StartupFilterEnabled"));

            StartupFilters.Add(
                LocalizationHelper.Get(
                    "StartupFilterDisabled"));

            SelectedFilterIndex =
                selectedIndex;

            OnPropertyChanged(
                nameof(StartupFilters));
        }

        private void SetUiState(
            StartupUiState state)
        {
            _currentUiState =
                state;

            ApplyUiState();
        }

        private void ApplyUiState()
        {
            switch (_currentUiState)
            {
                case StartupUiState.NotScanned:
                    ScanStatus =
                        LocalizationHelper.Get(
                            "StartupScanPrompt");

                    ScanBadgeText =
                        LocalizationHelper.Get(
                            "StartupBadgeNotScanned");

                    ScanBadgeBrush =
                        Brushes.LightGray;

                    break;

                case StartupUiState.Scanning:
                    ScanStatus =
                        LocalizationHelper.Get(
                            "StartupStatusScanning");

                    ScanBadgeText =
                        LocalizationHelper.Get(
                            "StartupBadgeScanning");

                    ScanBadgeBrush =
                        Brushes.Orange;

                    break;

                case StartupUiState.ScanCompleted:
                    ScanStatus =
                        LocalizationHelper.Format(
                            "StartupScanCompletedFormat",
                            StartupApplications.Count);

                    ScanBadgeText =
                        LocalizationHelper.Get(
                            "StartupBadgeScanned");

                    ScanBadgeBrush =
                        Brushes.LimeGreen;

                    break;

                case StartupUiState.ScanCompletedEmpty:
                    ScanStatus =
                        LocalizationHelper.Get(
                            "StartupScanCompletedEmpty");

                    ScanBadgeText =
                        LocalizationHelper.Get(
                            "StartupBadgeScanned");

                    ScanBadgeBrush =
                        Brushes.LimeGreen;

                    break;

                case StartupUiState.ScanFailed:
                    ScanStatus =
                        LocalizationHelper.Format(
                            "StartupScanFailedFormat",
                            _currentErrorMessage);

                    ScanBadgeText =
                        LocalizationHelper.Get(
                            "StartupBadgeError");

                    ScanBadgeBrush =
                        Brushes.OrangeRed;

                    break;

                case StartupUiState.EnablingApplication:
                    ScanStatus =
                        LocalizationHelper.Format(
                            "StartupEnablingFormat",
                            _currentApplicationName);

                    ScanBadgeText =
                        LocalizationHelper.Get(
                            "StartupBadgeChanging");

                    ScanBadgeBrush =
                        Brushes.Orange;

                    break;

                case StartupUiState.DisablingApplication:
                    ScanStatus =
                        LocalizationHelper.Format(
                            "StartupDisablingFormat",
                            _currentApplicationName);

                    ScanBadgeText =
                        LocalizationHelper.Get(
                            "StartupBadgeChanging");

                    ScanBadgeBrush =
                        Brushes.Orange;

                    break;

                case StartupUiState.ApplicationEnabled:
                    ScanStatus =
                        LocalizationHelper.Format(
                            "StartupEnabledFormat",
                            _currentApplicationName);

                    ScanBadgeText =
                        LocalizationHelper.Get(
                            "StartupBadgeChanged");

                    ScanBadgeBrush =
                        Brushes.LimeGreen;

                    break;

                case StartupUiState.ApplicationDisabled:
                    ScanStatus =
                        LocalizationHelper.Format(
                            "StartupDisabledFormat",
                            _currentApplicationName);

                    ScanBadgeText =
                        LocalizationHelper.Get(
                            "StartupBadgeChanged");

                    ScanBadgeBrush =
                        Brushes.LimeGreen;

                    break;

                case StartupUiState.ChangeFailed:
                    ScanStatus =
                        LocalizationHelper.Format(
                            "StartupChangeFailedFormat",
                            _currentErrorMessage);

                    ScanBadgeText =
                        LocalizationHelper.Get(
                            "StartupBadgeError");

                    ScanBadgeBrush =
                        Brushes.OrangeRed;

                    break;
            }
        }

        private void LanguageManager_LanguageChanged(
            object? sender,
            EventArgs e)
        {
            RefreshLocalizedFilters();

            foreach (StartupAppInfo application
                     in StartupApplications)
            {
                application
                    .RefreshLocalizedProperties();
            }

            OnPropertyChanged(
                nameof(ScanButtonText));

            ApplyUiState();

            StartupApplicationsView.Refresh();
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

        private enum StartupUiState
        {
            NotScanned,
            Scanning,
            ScanCompleted,
            ScanCompletedEmpty,
            ScanFailed,
            EnablingApplication,
            DisablingApplication,
            ApplicationEnabled,
            ApplicationDisabled,
            ChangeFailed
        }
    }
}