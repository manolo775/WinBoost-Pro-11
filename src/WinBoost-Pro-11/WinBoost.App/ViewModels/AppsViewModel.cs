using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using WinBoost.App.Commands;
using WinBoost.App.Localization;
using WinBoost.App.Models;
using WinBoost.App.Services.Apps;
using System.Windows;
using WinBoost.App.Helpers;

namespace WinBoost.App.ViewModels
{
    public class AppsViewModel : INotifyPropertyChanged
    {
        private readonly InstalledAppsScanner
            _installedAppsScanner;
        private readonly WingetUpdateService _wingetUpdateService;
        private bool _isWingetAvailable;
        private string _wingetVersion = string.Empty;
        private AppUpdateStatus _selectedAppUpdateStatus =
                  AppUpdateStatus.NotChecked;

        private string _selectedAppUpdateDetails =
            string.Empty;

        private bool _isCheckingSelectedAppUpdate;
        private bool _isUpdatingSelectedApp;
        private bool _isScanning;
        public bool IsWingetAvailable
        {
            get => _isWingetAvailable;

            private set
            {
                if (_isWingetAvailable == value)
                {
                    return;
                }

                _isWingetAvailable = value;

                OnPropertyChanged();

                CommandManager.InvalidateRequerySuggested();
            }
        }

        public string WingetVersion
        {
            get => _wingetVersion;

            private set
            {
                if (_wingetVersion == value)
                {
                    return;
                }

                _wingetVersion = value;

                OnPropertyChanged();
            }
        }

        public AppUpdateStatus SelectedAppUpdateStatus
        {
            get => _selectedAppUpdateStatus;

            private set
            {
                if (_selectedAppUpdateStatus == value)
                {
                    return;
                }

                _selectedAppUpdateStatus = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedAppUpdateStatusText));
                OnPropertyChanged(nameof(CanUpdateSelectedApp));

                CommandManager.InvalidateRequerySuggested();
            }
        }

        public string SelectedAppUpdateDetails
        {
            get => _selectedAppUpdateDetails;

            private set
            {
                if (_selectedAppUpdateDetails == value)
                    return;

                _selectedAppUpdateDetails = value;
                OnPropertyChanged();
                OnPropertyChanged(
                nameof(SelectedAppUpdateStatusText));
            }
        }

      
        public string SelectedAppUpdateStatusText =>
    SelectedAppUpdateStatus switch
    {
        AppUpdateStatus.Checking =>
            T("AppsUpdateChecking"),

        AppUpdateStatus.UpdateAvailable =>
            T("AppsUpdateAvailable"),

        AppUpdateStatus.Updating =>
            T("AppsUpdateUpdating"),

        AppUpdateStatus.Updated =>
            T("AppsUpdateCompleted"),

        AppUpdateStatus.UpToDate =>
            T("AppsUpdateUpToDate"),

        AppUpdateStatus.Unavailable =>
            T("AppsUpdateUnavailable"),

        AppUpdateStatus.Failed =>
            T("AppsUpdateFailed"),

        _ => string.Empty
    };
        public bool IsCheckingSelectedAppUpdate
        {
            get => _isCheckingSelectedAppUpdate;

            private set
            {
                if (_isCheckingSelectedAppUpdate == value)
                    return;

                _isCheckingSelectedAppUpdate = value;
                OnPropertyChanged();
             
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public bool IsUpdatingSelectedApp
        {
            get => _isUpdatingSelectedApp;

            private set
            {
                if (_isUpdatingSelectedApp == value)
                {
                    return;
                }

                _isUpdatingSelectedApp = value;

                OnPropertyChanged();
                OnPropertyChanged(
                    nameof(CanUpdateSelectedApp));

                CommandManager.InvalidateRequerySuggested();
            }
        }

        public bool CanUpdateSelectedApp =>
            SelectedAppUpdateStatus ==
                AppUpdateStatus.UpdateAvailable &&
            !IsUpdatingSelectedApp;

        private int _selectedSortIndex;
        private InstalledAppInfo? _selectedApplication;
        private string _searchText = string.Empty;
        private string _scanStatus = string.Empty;
        private string _scanBadgeText = string.Empty;
        private string _lastScanError = string.Empty;

        private Brush _scanBadgeBrush =
            Brushes.LightGray;

        private AppsScanState _scanState =
            AppsScanState.NotChecked;

        public AppsViewModel()
        {
            _installedAppsScanner =
                new InstalledAppsScanner();

            _wingetUpdateService =
                 new WingetUpdateService();

            Applications =
                new ObservableCollection<
                    InstalledAppInfo>();

            FilteredApplications =
                CollectionViewSource.GetDefaultView(
                    Applications);

            FilteredApplications.Filter =
                FilterApplication;

            ScanAppsCommand =
                new RelayCommand(
                    async _ => await ScanAppsAsync(),
                    _ => !IsScanning);

            OpenSelectedAppLocationCommand =
                 new RelayCommand(
                  _ => OpenSelectedAppLocation(),
                  _ => SelectedApplication?
                   .HasInstallLocation == true);

            CheckSelectedAppUpdateCommand =
                 new RelayCommand(
                     async _ =>
                        await CheckSelectedAppUpdateAsync(),
                         _ =>
                             SelectedApplication != null &&
                         IsWingetAvailable &&
                          !IsCheckingSelectedAppUpdate);

            UpdateSelectedAppCommand =
                     new RelayCommand(
                       async _ =>
                        await UpdateSelectedAppAsync(),
                        _ => CanUpdateSelectedApp);

            ApplySorting();
                 RefreshLocalizedScanTexts();
                  _ = CheckWingetAvailabilityAsync();

            LanguageManager.Instance.LanguageChanged +=
                (_, _) =>
                {
                    RefreshLocalizedScanTexts();

                    OnPropertyChanged(
                        nameof(ScanButtonText));

                    OnPropertyChanged(
                        nameof(SelectedAppUpdateStatusText));
              
                };
        }

        public ObservableCollection<
            InstalledAppInfo> Applications
        {
            get;
        }

        public ICollectionView FilteredApplications
        {
            get;
        }

        public ICommand ScanAppsCommand
        {
            get;
        }

        public ICommand OpenSelectedAppLocationCommand
        {
            get;
        }

        public ICommand CheckSelectedAppUpdateCommand { get; }

        public ICommand UpdateSelectedAppCommand { get; }

        public InstalledAppInfo? SelectedApplication
        {
            get => _selectedApplication;

            set
            {
                if (_selectedApplication == value)
                    return;

                _selectedApplication = value;

                SelectedAppUpdateStatus =
                    AppUpdateStatus.NotChecked;

                SelectedAppUpdateDetails =
                    string.Empty;

                OnPropertyChanged();

                CommandManager.InvalidateRequerySuggested();
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

                FilteredApplications.Refresh();
            }
        }

        public int SelectedSortIndex
        {
            get => _selectedSortIndex;

            set
            {
                if (_selectedSortIndex == value)
                {
                    return;
                }

                _selectedSortIndex = value;

                OnPropertyChanged();

                ApplySorting();
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

        public string ScanBadgeText
        {
            get => _scanBadgeText;

            private set
            {
                if (_scanBadgeText == value)
                {
                    return;
                }

                _scanBadgeText = value;

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

                _scanBadgeBrush = value;

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

                _isScanning = value;

                OnPropertyChanged();

                OnPropertyChanged(
                    nameof(ScanButtonText));

                CommandManager
                    .InvalidateRequerySuggested();
            }
        }

        public string ScanButtonText =>
            IsScanning
                ? T("AppsScanningButton")
                : T("AppsScanButton");

        private static string T(
            string key,
            params object[] arguments)
        {
            return LocalizationHelper.Format(
                key,
                arguments);
        }

        private async Task CheckWingetAvailabilityAsync()
        {
            WingetAvailabilityResult result =
                await _wingetUpdateService
                    .CheckAvailabilityAsync();

            IsWingetAvailable =
                result.IsAvailable;

            WingetVersion =
                result.Version;
        }
        private bool FilterApplication(
            object item)
        {
            if (item is not InstalledAppInfo
                application)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(
                    SearchText))
            {
                return true;
            }

            string search =
                SearchText.Trim();

            return ContainsText(
                       application.DisplayName,
                       search) ||
                   ContainsText(
                       application.Publisher,
                       search) ||
                   ContainsText(
                       application.Version,
                       search) ||
                   ContainsText(
                       application.InstallDate,
                       search);
        }

        private static bool ContainsText(
            object? value,
            string search)
        {
            string text =
                Convert.ToString(value) ??
                string.Empty;

            return text.Contains(
                search,
                StringComparison
                    .OrdinalIgnoreCase);
        }

        private void ApplySorting()
        {
            if (FilteredApplications is
                not ListCollectionView view)
            {
                return;
            }

            view.CustomSort =
                new InstalledAppsComparer(
                    SelectedSortIndex);

            view.Refresh();
        }

        private async Task CheckSelectedAppUpdateAsync()
        {
            if (SelectedApplication == null ||
                !IsWingetAvailable ||
                IsCheckingSelectedAppUpdate)
            {
                return;
            }

            IsCheckingSelectedAppUpdate = true;

            SelectedAppUpdateStatus =
                AppUpdateStatus.Checking;

            SelectedAppUpdateDetails =
                string.Empty;

            try
            {
                AppUpdateCheckResult result =
                    await _wingetUpdateService
                        .CheckForUpdateAsync(
                            SelectedApplication.DisplayName);

                SelectedAppUpdateStatus =
                    result.Status;

                SelectedAppUpdateDetails =
                    result.Details;
            }
            finally
            {
                IsCheckingSelectedAppUpdate = false;
            }
        }

        private async Task UpdateSelectedAppAsync()
        {
            if (!CanUpdateSelectedApp ||
                SelectedApplication == null)
            {
                return;
            }

            bool confirmed =
                NativeConfirmationDialog.Ask(
                    Application.Current.MainWindow,
                    T("AppsUpdateConfirmationTitle"),
                    T(
                        "AppsUpdateConfirmationMessage",
                        SelectedApplication.DisplayName),
                    T("AppsUpdateConfirmYes"),
                    T("AppsUpdateConfirmNo"));

            if (!confirmed)
            {
                return;
            }

            IsUpdatingSelectedApp = true;

            SelectedAppUpdateStatus =
                AppUpdateStatus.Updating;

            try
            {
                AppUpdateCheckResult result =
                    await _wingetUpdateService.UpdateAsync(
                        SelectedApplication.DisplayName);

                SelectedAppUpdateStatus =
                    result.Status;

                SelectedAppUpdateDetails =
                    result.Details;
            }
            finally
            {
                IsUpdatingSelectedApp = false;
            }
        }

        private void OpenSelectedAppLocation()
        {
            if (SelectedApplication == null ||
                !SelectedApplication.HasInstallLocation)
            {
                return;
            }

            string location =
                SelectedApplication.InstallLocation;

            if (!System.IO.Directory.Exists(
                    location))
            {
                return;
            }

            Process.Start(
                new ProcessStartInfo
                {
                    FileName = location,
                    UseShellExecute = true
                });
        }
        private async Task ScanAppsAsync()
        {
            if (IsScanning)
            {
                return;
            }

            IsScanning = true;

            _scanState =
                AppsScanState.Scanning;

            RefreshLocalizedScanTexts();

            try
            {
                var applications =
                    await _installedAppsScanner
                        .ScanAsync();

                Applications.Clear();

                foreach (InstalledAppInfo
                         application
                         in applications)
                {
                    Applications.Add(
                        application);
                }

                _lastScanError =
                    string.Empty;

                _scanState =
                    AppsScanState.Completed;

                ApplySorting();

                RefreshLocalizedScanTexts();
            }
            catch (Exception ex)
            {
                _lastScanError =
                    ex.Message;

                _scanState =
                    AppsScanState.Error;

                RefreshLocalizedScanTexts();
            }
            finally
            {
                IsScanning = false;
            }
        }

        private void RefreshLocalizedScanTexts()
        {
            switch (_scanState)
            {
                case AppsScanState.Scanning:
                    ScanStatus =
                        T("AppsScanInProgress");

                    ScanBadgeText =
                        T("AppsScanBadgeScanning");

                    ScanBadgeBrush =
                        Brushes.Orange;

                    break;

                case AppsScanState.Completed:
                    ScanStatus =
                        T(
                            "AppsScanCompleted",
                            Applications.Count);

                    ScanBadgeText =
                        T("AppsScanBadgeCompleted");

                    ScanBadgeBrush =
                        Brushes.LimeGreen;

                    break;

                case AppsScanState.Error:
                    ScanStatus =
                        T(
                            "AppsScanFailed",
                            _lastScanError);

                    ScanBadgeText =
                        T("AppsScanBadgeError");

                    ScanBadgeBrush =
                        Brushes.OrangeRed;

                    break;

                default:
                    ScanStatus =
                        T("AppsScanInitialStatus");

                    ScanBadgeText =
                        T("AppsScanBadgeNotChecked");

                    ScanBadgeBrush =
                        Brushes.LightGray;

                    break;
            }
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

        private enum AppsScanState
        {
            NotChecked,
            Scanning,
            Completed,
            Error
        }

        private sealed class
            InstalledAppsComparer : IComparer
        {
            private readonly int _sortIndex;

            public InstalledAppsComparer(
                int sortIndex)
            {
                _sortIndex = sortIndex;
            }

            public int Compare(
                object? first,
                object? second)
            {
                if (first is not InstalledAppInfo
                    firstApplication ||
                    second is not InstalledAppInfo
                    secondApplication)
                {
                    return 0;
                }

                int result;

                switch (_sortIndex)
                {
                    case 1:
                        result = CompareText(
                            firstApplication.Publisher,
                            secondApplication.Publisher);

                        break;

                    case 2:
                        result =
                            secondApplication
                                .InstallDateValue
                                .CompareTo(
                                    firstApplication
                                        .InstallDateValue);

                        break;

                    default:
                        result = CompareText(
                            firstApplication.DisplayName,
                            secondApplication.DisplayName);

                        break;
                }

                return result != 0
                    ? result
                    : CompareText(
                        firstApplication.DisplayName,
                        secondApplication.DisplayName);
            }

            private static int CompareText(
                string first,
                string second)
            {
                return string.Compare(
                    first,
                    second,
                    StringComparison
                        .CurrentCultureIgnoreCase);
            }
        }
    }
}