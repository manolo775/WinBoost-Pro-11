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
    : INotifyPropertyChanged
    {
        private bool _isSelected;

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

        public string UpdateId
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

        public string AdvisorCategory
        {
            get;
            init;
        } = string.Empty;

        public string AdvisorRecommendation
        {
            get;
            init;
        } = string.Empty;

        public bool IsHighPriority
        {
            get;
            init;
        }

        public bool IsSelected
        {
            get => _isSelected;

            set
            {
                if (_isSelected == value)
                {
                    return;
                }

                _isSelected = value;

                PropertyChanged?.Invoke(
                    this,
                    new PropertyChangedEventArgs(
                        nameof(IsSelected)));
            }
        }

        public event PropertyChangedEventHandler?
            PropertyChanged;
    }

    public class WindowsUpdateViewModel : INotifyPropertyChanged
    {
        private readonly WindowsUpdateScanner
            _windowsUpdateScanner;

        private readonly WindowsUpdateAvailableScanner
            _windowsUpdateAvailableScanner;

        private readonly WindowsUpdateAdvisor
             _windowsUpdateAdvisor;

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

            _windowsUpdateAdvisor =
                  new WindowsUpdateAdvisor();

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

            SelectRecommendedUpdatesCommand =
    new RelayCommand(
        _ =>
            SelectRecommendedUpdates());

            ClearUpdateSelectionCommand =
                new RelayCommand(
                    _ =>
                        ClearUpdateSelection());

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

        public ICommand SelectRecommendedUpdatesCommand
        {
            get;
        }

        public ICommand ClearUpdateSelectionCommand
        {
            get;
        }

        public ObservableCollection<
            WindowsUpdateAvailableDisplayItem>
            AvailableUpdates
        {
            get;
        }

        public int SecurityUpdateCount =>
             CountUpdatesByType(
             WindowsUpdateAdvisorType.Security);

        public int SystemUpdateCount =>
            CountUpdatesByType(
                WindowsUpdateAdvisorType.System);

        public int DriverUpdateCount =>
            CountUpdatesByType(
                WindowsUpdateAdvisorType.Driver);

        public int OptionalUpdateCount =>
            CountUpdatesByType(
                WindowsUpdateAdvisorType.Optional);

        public int RecommendedUpdateCount =>
            CountRecommendedUpdates();

        public int HighPriorityUpdateCount =>
            CountHighPriorityUpdates();

        public string AdvisorState
        {
            get
            {
                if (IsScanning)
                {
                    return "Checking";
                }

                if (ScanState == "Error")
                {
                    return "Error";
                }

                if (_lastScanResult == null)
                {
                    return "NotChecked";
                }

                if (_lastScanResult.DisabledServices.Count > 0 ||
                    HighPriorityUpdateCount > 0)
                {
                    return "ActionRecommended";
                }

                if (RecommendedUpdateCount > 0 ||
                    OptionalUpdateCount > 0 ||
                    DriverUpdateCount > 0 ||
                    _lastScanResult.StoppedServices.Count > 0)
                {
                    return "Attention";
                }

                return "Good";
            }
        }

        public string AdvisorStatusText =>
            AdvisorState switch
            {
                "Checking" =>
                    LocalizationHelper.Get(
                        "WindowsUpdateAdvisorStatusChecking"),

                "Good" =>
                    LocalizationHelper.Get(
                        "WindowsUpdateAdvisorStatusGood"),

                "Attention" =>
                    LocalizationHelper.Get(
                        "WindowsUpdateAdvisorStatusAttention"),

                "ActionRecommended" =>
                    LocalizationHelper.Get(
                        "WindowsUpdateAdvisorStatusActionRecommended"),

                "Error" =>
                    LocalizationHelper.Get(
                        "WindowsUpdateAdvisorStatusError"),

                _ =>
                    LocalizationHelper.Get(
                        "WindowsUpdateAdvisorStatusNotChecked")
            };

        public string AdvisorMessage
        {
            get
            {
                if (IsScanning)
                {
                    return LocalizationHelper.Get(
                        "WindowsUpdateAdvisorMessageChecking");
                }

                if (ScanState == "Error")
                {
                    return LocalizationHelper.Format(
                        "WindowsUpdateAdvisorMessageError",
                        _lastErrorMessage);
                }

                if (_lastScanResult == null)
                {
                    return LocalizationHelper.Get(
                        "WindowsUpdateAdvisorMessageNotChecked");
                }

                if (_lastScanResult.DisabledServices.Count > 0)
                {
                    return LocalizationHelper.Format(
                        "WindowsUpdateAdvisorMessageDisabledServices",
                        string.Join(
                            ", ",
                            _lastScanResult.DisabledServices));
                }

                if (RecommendedUpdateCount > 0)
                {
                    return LocalizationHelper.Format(
                        "WindowsUpdateAdvisorMessageRecommendedUpdates",
                        RecommendedUpdateCount);
                }

                if (OptionalUpdateCount > 0 ||
                    DriverUpdateCount > 0)
                {
                    return LocalizationHelper.Format(
                        "WindowsUpdateAdvisorMessageOptionalUpdates",
                        DriverUpdateCount,
                        OptionalUpdateCount);
                }

                if (_lastScanResult.StoppedServices.Count > 0)
                {
                    return LocalizationHelper.Get(
                        "WindowsUpdateAdvisorMessageServicesOnDemand");
                }

                return LocalizationHelper.Get(
                    "WindowsUpdateAdvisorMessageGood");
            }
        }

        public string AdvisorRecommendation
        {
            get
            {
                if (IsScanning)
                {
                    return LocalizationHelper.Get(
                        "WindowsUpdateAdvisorRecommendationWait");
                }

                if (ScanState == "Error")
                {
                    return LocalizationHelper.Get(
                        "WindowsUpdateAdvisorRecommendationRetry");
                }

                if (_lastScanResult == null)
                {
                    return LocalizationHelper.Get(
                        "WindowsUpdateAdvisorRecommendationScan");
                }

                if (_lastScanResult.DisabledServices.Count > 0)
                {
                    return LocalizationHelper.Get(
                        "WindowsUpdateAdvisorRecommendationServices");
                }

                if (RecommendedUpdateCount > 0)
                {
                    return LocalizationHelper.Format(
                        "WindowsUpdateAdvisorRecommendationInstall",
                        RecommendedUpdateCount);
                }

                if (OptionalUpdateCount > 0 ||
                    DriverUpdateCount > 0)
                {
                    return LocalizationHelper.Get(
                        "WindowsUpdateAdvisorRecommendationReviewOptional");
                }

                return LocalizationHelper.Get(
                    "WindowsUpdateAdvisorRecommendationGood");
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

                NotifyAdvisorTextProperties();

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
            HasSelectedUpdates();

        public bool HasAvailableUpdates =>
              AvailableUpdates.Count > 0;

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

                NotifyAdvisorSummaryProperties();

                _lastScanResult =
                    result;

                _lastAvailableUpdateCount =
                    availableResult.UpdateCount;

                OnPropertyChanged(
                    nameof(HasAvailableUpdates));

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

                NotifyAdvisorSummaryProperties();

                _lastAvailableUpdateCount =
                    0;

                AvailableUpdates.Clear();

                OnPropertyChanged(
                    nameof(HasAvailableUpdates));

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

        private bool HasSelectedUpdates()
        {
            foreach (
                WindowsUpdateAvailableDisplayItem update
                in AvailableUpdates)
            {
                if (update.IsSelected)
                {
                    return true;
                }
            }

            return false;
        }

        private void SelectRecommendedUpdates()
        {
            foreach (
                WindowsUpdateAvailableDisplayItem item
                in AvailableUpdates)
            {
                WindowsUpdateAvailableInfo? update =
                    _lastAvailableUpdates
                        .FirstOrDefault(
                            candidate =>
                                string.Equals(
                                    candidate.UpdateId,
                                    item.UpdateId,
                                    StringComparison.OrdinalIgnoreCase));

                if (update == null)
                {
                    item.IsSelected = false;
                    continue;
                }

                WindowsUpdateAdvisorResult advisorResult =
                    _windowsUpdateAdvisor
                        .Analyze(update);

                item.IsSelected =
                    advisorResult.IsRecommended;
            }

            OnPropertyChanged(
                nameof(CanInstallUpdates));

            CommandManager
                .InvalidateRequerySuggested();
        }

        private void ClearUpdateSelection()
        {
            foreach (
                WindowsUpdateAvailableDisplayItem item
                in AvailableUpdates)
            {
                item.IsSelected =
                    false;
            }

            OnPropertyChanged(
                nameof(CanInstallUpdates));

            CommandManager
                .InvalidateRequerySuggested();
        }

        private async Task ConfirmInstallUpdatesAsync()
        {
            if (!CanInstallUpdates)
            {
                return;
            }

            var selectedUpdateIds =
                new List<string>();

            foreach (
                WindowsUpdateAvailableDisplayItem update
                in AvailableUpdates)
            {
                if (!update.IsSelected)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(
                        update.UpdateId))
                {
                    continue;
                }

                selectedUpdateIds.Add(
                    update.UpdateId);
            }

            if (selectedUpdateIds.Count == 0)
            {
                MessageBox.Show(
        LocalizationHelper.Get(
            "WindowsUpdateSelectAtLeastOne"),
        "WinBoost Pro 11",
        MessageBoxButton.OK,
        MessageBoxImage.Information);

                return;
            }

            bool confirmed =
               NativeConfirmationDialog.Ask(
            Application.Current.MainWindow,
                  LocalizationHelper.Get(
                 "WindowsUpdateInstallSelectedConfirmationTitle"),
                    selectedUpdateIds.Count == 1
                    ? LocalizationHelper.Get(
                       "WindowsUpdateInstallSelectedConfirmationMessageSingular")
                    : LocalizationHelper.Format(
                     "WindowsUpdateInstallSelectedConfirmationMessage",
                  selectedUpdateIds.Count),
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

                string currentLanguage =
                    LanguageManager.Instance.CurrentLanguage ==
                         Language.Romanian
                            ? "ro"
                             : "en";
                startInfo.ArgumentList.Add(
                    $"--language={currentLanguage}");

                foreach (string updateId
                         in selectedUpdateIds)
                {
                    startInfo.ArgumentList.Add(
                        $"--update-id={updateId}");
                }

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
                         LocalizationHelper.Get(
                         "WindowsUpdateWorkerFinished");

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
                      LocalizationHelper.Get(
                      "WindowsUpdateWorkerFinished");

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
                               ? LocalizationHelper.Get(
                                  "WindowsUpdateInstallSuccessRestart")
                                : LocalizationHelper.Get(
                                  "WindowsUpdateInstallSuccess");
                    }

                    await Task.Delay(
                        TimeSpan.FromSeconds(2));

                    IsInstallingUpdates =
                        false;

                    await ScanUpdatesAsync();

                    return;
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

        private int CountUpdatesByType(
            WindowsUpdateAdvisorType type)
        {
            int count = 0;

            foreach (WindowsUpdateAvailableInfo update
                     in _lastAvailableUpdates)
            {
                WindowsUpdateAdvisorResult result =
                    _windowsUpdateAdvisor
                        .Analyze(update);

                if (result.Type == type)
                {
                    count++;
                }
            }

            return count;
        }

        private int CountRecommendedUpdates()
        {
            int count = 0;

            foreach (WindowsUpdateAvailableInfo update
                     in _lastAvailableUpdates)
            {
                WindowsUpdateAdvisorResult result =
                    _windowsUpdateAdvisor
                        .Analyze(update);

                if (result.IsRecommended)
                {
                    count++;
                }
            }

            return count;
        }

        private int CountHighPriorityUpdates()
        {
            int count = 0;

            foreach (WindowsUpdateAvailableInfo update
                     in _lastAvailableUpdates)
            {
                WindowsUpdateAdvisorResult result =
                    _windowsUpdateAdvisor
                        .Analyze(update);

                if (result.IsHighPriority)
                {
                    count++;
                }
            }

            return count;
        }

        private void NotifyAdvisorSummaryProperties()
        {
            OnPropertyChanged(
                nameof(SecurityUpdateCount));

            OnPropertyChanged(
                nameof(SystemUpdateCount));

            OnPropertyChanged(
                nameof(DriverUpdateCount));

            OnPropertyChanged(
                nameof(OptionalUpdateCount));

            OnPropertyChanged(
                nameof(RecommendedUpdateCount));

            OnPropertyChanged(
                nameof(HighPriorityUpdateCount));

            NotifyAdvisorTextProperties();
        }

        private void NotifyAdvisorTextProperties()
        {
            OnPropertyChanged(
                nameof(AdvisorState));

            OnPropertyChanged(
                nameof(AdvisorStatusText));

            OnPropertyChanged(
                nameof(AdvisorMessage));

            OnPropertyChanged(
                nameof(AdvisorRecommendation));
        }

        private void RefreshAvailableUpdates()
        {
            bool hadExistingItems =
              AvailableUpdates.Count > 0;

            var selectedUpdateIds =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);

            foreach (
                WindowsUpdateAvailableDisplayItem existingItem
                in AvailableUpdates)
            {
                if (existingItem.IsSelected &&
                    !string.IsNullOrWhiteSpace(
                        existingItem.UpdateId))
                {
                    selectedUpdateIds.Add(
                        existingItem.UpdateId);
                }
            }

            AvailableUpdates.Clear();

            foreach (
                WindowsUpdateAvailableInfo update
                in _lastAvailableUpdates)
            {
                WindowsUpdateAdvisorResult advisorResult =
                    _windowsUpdateAdvisor
                        .Analyze(update);

                bool isSelectedByDefault =
                     advisorResult.IsRecommended;

                bool isSelected =
                    hadExistingItems
                        ? selectedUpdateIds.Contains(
                            update.UpdateId)
                        : isSelectedByDefault;

                update.IsSelected =
                    isSelected;

                string advisorCategory =
    advisorResult.Type switch
    {
        WindowsUpdateAdvisorType.Security =>
            LocalizationHelper.Get(
                "WindowsUpdateAdvisorCategorySecurity"),

        WindowsUpdateAdvisorType.System =>
            LocalizationHelper.Get(
                "WindowsUpdateAdvisorCategorySystem"),

        WindowsUpdateAdvisorType.Driver =>
            LocalizationHelper.Get(
                "WindowsUpdateAdvisorCategoryDriver"),

        WindowsUpdateAdvisorType.Optional =>
            LocalizationHelper.Get(
                "WindowsUpdateAdvisorCategoryOptional"),

        _ =>
            LocalizationHelper.Get(
                "WindowsUpdateAdvisorCategoryOther")
    };

                string advisorRecommendation =
                    advisorResult.IsHighPriority
                        ? LocalizationHelper.Get(
                            "WindowsUpdateAdvisorItemHighPriority")
                        : advisorResult.IsRecommended
                            ? LocalizationHelper.Get(
                                "WindowsUpdateAdvisorItemRecommended")
                            : LocalizationHelper.Get(
                                "WindowsUpdateAdvisorItemReview");

                var displayItem =
                    new WindowsUpdateAvailableDisplayItem
                    {
                        Title =
                            update.Title,

                        Description =
                            update.Description,

                        UpdateId =
                            update.UpdateId,

                        IsDownloaded =
                            GetLocalizedBoolean(
                                update.IsDownloaded),

                        RebootRequired =
                            GetLocalizedBoolean(
                                update.RebootRequired),

                        AdvisorCategory =
                            advisorCategory,

                        AdvisorRecommendation =
                            advisorRecommendation,

                        IsHighPriority =
                            advisorResult.IsHighPriority,

                        IsSelected =
                            isSelected
                    };

                displayItem.PropertyChanged +=
                    AvailableUpdate_PropertyChanged;

                AvailableUpdates.Add(
                    displayItem);
            }

            OnPropertyChanged(
                nameof(CanInstallUpdates));

            CommandManager
                .InvalidateRequerySuggested();
        }

        private static string GetLocalizedBoolean(
            bool value)
        {
            return LocalizationHelper.Get(
                value
                    ? "WindowsUpdateYes"
                    : "WindowsUpdateNo");
        }

        private void AvailableUpdate_PropertyChanged(
    object? sender,
    PropertyChangedEventArgs e)
        {
            if (e.PropertyName !=
                nameof(
                    WindowsUpdateAvailableDisplayItem
                        .IsSelected))
            {
                return;
            }

            OnPropertyChanged(
                nameof(CanInstallUpdates));

            CommandManager
                .InvalidateRequerySuggested();
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
            NotifyAdvisorSummaryProperties();

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