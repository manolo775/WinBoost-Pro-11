using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WinBoost.App.Commands;
using WinBoost.App.Helpers;
using WinBoost.App.Localization;
using WinBoost.App.Models;
using WinBoost.App.Services.Recovery;
using System.Text.Json;
using System.IO;
using System.Xml.Linq;
using System.Linq;

namespace WinBoost.App.ViewModels
{
    public sealed class RecoveryViewModel : INotifyPropertyChanged
    {
        private const uint ServiceDisabledHResult =
            0x80070422u;

        private static readonly string
              RestorePointsCachePath =
             Path.Combine(
              Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
               "WinBoost Pro 11",
              "Recovery",
               "restore-points-cache.json");

        private static readonly string
    PendingRestorePath =
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            "WinBoost Pro 11",
            "Recovery",
            "pending-restore.json");

        private readonly SystemRestorePointService
            _restorePointService;

        private readonly SystemRestorePointScanner
            _restorePointScanner;

        private readonly AsyncRelayCommand
            _createRestorePointCommand;

        private readonly AsyncRelayCommand
           _refreshRestorePointsCommand;

        private readonly AsyncRelayCommand
            _enableSystemProtectionCommand;

        private readonly AsyncRelayCommand
            _restoreSystemCommand;

        private readonly AsyncRelayCommand
            _restartNowCommand;

        private readonly AsyncRelayCommand
            _restartLaterCommand;

        private bool _isCheckingAvailability;

        private bool _isCreatingRestorePoint;

        private bool _isEnablingSystemProtection;

        private bool _isLoadingRestorePoints;

        private bool _isRestoringSystem;

        private bool _isRestartRequired;

        private bool _isSystemRestoreAvailable;

        private bool _isSystemProtectionActionRequired;

        private SystemRestorePointInfo?
            _selectedRestorePoint;

        private string _availabilityMessage =
            string.Empty;

        private string _statusMessage =
            string.Empty;

        private string? _statusMessageResourceKey;

        private object[] _statusMessageArguments =
            Array.Empty<object>();

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

            _refreshRestorePointsCommand =
      new AsyncRelayCommand(
          RefreshRestorePointsAsync,
          () =>
              !IsLoadingRestorePoints);

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

            _restartNowCommand =
                new AsyncRelayCommand(
                    RestartNowAsync,
                    () =>
                        CanRestartNow);

            _restartLaterCommand =
                new AsyncRelayCommand(
                    RestartLaterAsync,
                    () =>
                        CanRestartLater);

            CreateRestorePointCommand =
                _createRestorePointCommand;

            RefreshRestorePointsCommand =
                _refreshRestorePointsCommand;

            EnableSystemProtectionCommand =
                _enableSystemProtectionCommand;

            RestoreSystemCommand =
                _restoreSystemCommand;

            RestartNowCommand =
                _restartNowCommand;

            RestartLaterCommand =
                _restartLaterCommand;

            WeakEventManager<LanguageManager, EventArgs>
             .AddHandler(
             LanguageManager.Instance,
              nameof(LanguageManager.LanguageChanged),
              OnLanguageChanged);

        }


        public ICommand CreateRestorePointCommand
        {
            get;
        }

        public ICommand RefreshRestorePointsCommand
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

        public ICommand RestartNowCommand
        {
            get;
        }

        public ICommand RestartLaterCommand
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



                NotifyCommandStates();
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


        public bool IsRestartRequired
        {
            get => _isRestartRequired;

            private set
            {
                if (_isRestartRequired == value)
                {
                    return;
                }

                _isRestartRequired =
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
            !IsRestoringSystem &&
            !IsRestartRequired;


        public bool CanEnableSystemProtection =>
            IsSystemRestoreAvailable &&
            IsSystemProtectionActionRequired &&
            !IsCheckingAvailability &&
            !IsCreatingRestorePoint &&
            !IsEnablingSystemProtection &&
            !IsLoadingRestorePoints &&
            !IsRestoringSystem &&
            !IsRestartRequired;


        public bool CanRestoreSystem =>
            IsSystemRestoreAvailable &&
            SelectedRestorePoint != null &&
            !IsCheckingAvailability &&
            !IsCreatingRestorePoint &&
            !IsEnablingSystemProtection &&
            !IsLoadingRestorePoints &&
            !IsRestoringSystem &&
            !IsRestartRequired;


        public bool CanRestartNow =>
            IsRestartRequired &&
            !IsRestoringSystem;


        public bool CanRestartLater =>
            IsRestartRequired &&
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


        // ======================================
        // CHECK AVAILABILITY
        // ======================================

        public async Task CheckAvailabilityAsync()
        {
            if (IsCheckingAvailability)
            {
                return;
            }

            IsCheckingAvailability =
                true;

            AvailabilityMessage =
                LocalizationHelper.Get(
                    "RecoveryCheckingAvailability");

            try
            {
                SystemRestoreAvailabilityResult result =
                    await _restorePointService
                        .CheckAvailabilityAsync();

                IsSystemRestoreAvailable =
                    result.IsAvailable;

                /*
                 * Mesajul serviciului rămâne folosit momentan
                 * deoarece conține și eventualele detalii WMI.
                 */
                AvailabilityMessage =
                          result.IsAvailable
                         ? LocalizationHelper.Get(
                          "RecoverySystemRestoreAvailable")
                         : LocalizationHelper.Get(
                          "RecoverySystemRestoreUnavailable");
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


        // ======================================
        // LOAD RESTORE POINTS
        // ======================================


        private async Task RefreshRestorePointsAsync()
        {
            ClearStatusMessage();

            await LoadRestorePointsAsync();
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
                LocalizationHelper.Get(
                    "RecoveryLoadingRestorePoints");

            string? outputPath =
                null;

            try
            {
                string workerPath =
                    System.IO.Path.Combine(
                        AppContext.BaseDirectory,
                        "WinBoost.RecoveryWorker.exe");

                if (!System.IO.File.Exists(
                        workerPath))
                {
                    throw new System.IO.FileNotFoundException(
                        "WinBoost Recovery Worker was not found.",
                        workerPath);
                }

                outputPath =
                    System.IO.Path.Combine(
                        System.IO.Path.GetTempPath(),
                        $"WinBoost-RestorePoints-" +
                        $"{Guid.NewGuid():N}.json");

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

                startInfo.ArgumentList.Add(
                    "--list");

                startInfo.ArgumentList.Add(
                    $"--output={outputPath}");

                Process? workerProcess =
                    Process.Start(
                        startInfo);

                if (workerProcess == null)
                {
                    throw new InvalidOperationException(
                        "WinBoost Recovery Worker could not be started.");
                }

                await workerProcess
                    .WaitForExitAsync();

                if (workerProcess.ExitCode != 0)
                {
                    throw new InvalidOperationException(
                        $"Recovery Worker exit code: " +
                        $"{workerProcess.ExitCode}");
                }

                if (!System.IO.File.Exists(
                        outputPath))
                {
                    throw new System.IO.FileNotFoundException(
                        "Recovery Worker did not create the restore point list.");
                }

                string json =
                    await System.IO.File
                        .ReadAllTextAsync(
                            outputPath);

                List<RestorePointWorkerItem>?
                    restorePoints =
                        JsonSerializer.Deserialize<
                            List<RestorePointWorkerItem>>(
                                json);

                if (restorePoints != null)
                {
                    await SaveRestorePointsCacheAsync(
                        restorePoints);
                }

                RestorePoints.Clear();

                if (restorePoints != null)
                {
                    foreach (RestorePointWorkerItem
                             restorePoint in restorePoints)
                    {
                        RestorePoints.Add(
                            new SystemRestorePointInfo
                            {
                                SequenceNumber =
                                    restorePoint.SequenceNumber,

                                Description =
                                    restorePoint.Description,

                                RestorePointType =
                                    restorePoint.RestorePointType,

                                RestorePointTypeName =
                                    GetRestorePointTypeName(
                                        restorePoint.RestorePointType),

                                CreatedAt =
                                    restorePoint.CreatedAt
                            });
                    }
                }

                SelectedRestorePoint =
                    RestorePoints.Count > 0
                        ? RestorePoints[0]
                        : null;

                RestorePointsMessage =
                    RestorePoints.Count == 0
                        ? LocalizationHelper.Get(
                            "RecoveryNoRestorePoints")
                        : LocalizationHelper.Format(
                            "RecoveryRestorePointsFound",
                            RestorePoints.Count);
            }
            catch (System.ComponentModel.Win32Exception ex)
                when (ex.NativeErrorCode == 1223)
            {
                RestorePointsMessage =
                    LocalizationHelper.Get(
                        "RecoveryNoRestorePoints");
            }
            catch (Exception ex)
            {
                RestorePoints.Clear();

                SelectedRestorePoint =
                    null;

                RestorePointsMessage =
                    LocalizationHelper.Format(
                        "RecoveryRestorePointsLoadFailed",
                        ex.Message);
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(
                        outputPath))
                {
                    try
                    {
                        if (System.IO.File.Exists(
                                outputPath))
                        {
                            System.IO.File.Delete(
                                outputPath);
                        }
                    }
                    catch
                    {
                        // Temporary file cleanup must not
                        // interrupt the Recovery page.
                    }
                }

                IsLoadingRestorePoints =
                    false;
            }
        }


        // ======================================
        // CREATE RESTORE POINT
        // ======================================

        private async Task CreateRestorePointAsync()
        {
            if (!CanCreateRestorePoint)
            {
                return;
            }

            IsCreatingRestorePoint =
                true;

            SetLocalizedStatusMessage(
                "RecoveryCreatingRestorePoint");

            try
            {
                DateTime createdAt =
                    DateTime.Now;

                string restorePointDescription =
                    $"WinBoost Pro 11 - Safety Restore Point - " +
                    $"{createdAt:dd.MM.yyyy HH:mm:ss}";

                string workerPath =
                    System.IO.Path.Combine(
                        AppContext.BaseDirectory,
                        "WinBoost.RecoveryWorker.exe");

                if (!System.IO.File.Exists(
                        workerPath))
                {
                    SetLocalizedStatusMessage(
                        "RecoveryRestorePointError",
                        $"Recovery worker not found: {workerPath}");

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

                startInfo.ArgumentList.Add(
                    "--create");

                startInfo.ArgumentList.Add(
                    $"--description={restorePointDescription}");

                Process? workerProcess =
                    Process.Start(
                        startInfo);

                if (workerProcess == null)
                {
                    SetLocalizedStatusMessage(
                        "RecoveryRestorePointError",
                        "WinBoost Recovery Worker could not be started.");

                    return;
                }

                await workerProcess
                    .WaitForExitAsync();

                if (workerProcess.ExitCode == 21)
                {
                    SetLocalizedStatusMessage(
                        "RecoveryNoNewRestorePoint");

                    await LoadRestorePointsAsync();

                    return;
                }

                if (workerProcess.ExitCode != 0)
                {
                    SetLocalizedStatusMessage(
                        "RecoveryRestorePointCreateFailed",
                        $"Recovery Worker exit code: " +
                        $"{workerProcess.ExitCode}");

                    return;
                }

                IsSystemProtectionActionRequired =
                    false;

                SetLocalizedStatusMessage(
                    "RecoveryRestorePointCreatedWithDate",
                    createdAt.ToString(
                        "dd.MM.yyyy HH:mm:ss"));

                await Task.Delay(
                    TimeSpan.FromSeconds(2));

                await LoadRestorePointsAsync();
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                if (ex.NativeErrorCode == 1223)
                {
                    ClearStatusMessage();

                    return;
                }

                SetLocalizedStatusMessage(
                    "RecoveryRestorePointError",
                    ex.Message);
            }
            catch (Exception ex)
            {
                SetLocalizedStatusMessage(
                    "RecoveryRestorePointError",
                    ex.Message);
            }
            finally
            {
                IsCreatingRestorePoint =
                    false;
            }
        }


        // ======================================
        // ENABLE SYSTEM PROTECTION
        // ======================================

        private async Task EnableSystemProtectionAsync()
        {
            if (!CanEnableSystemProtection)
            {
                return;
            }

            IsEnablingSystemProtection =
                true;

            SetLocalizedStatusMessage(
                "RecoveryEnablingSystemProtection");

            try
            {
                SystemProtectionResult result =
                    await _restorePointService
                        .EnableSystemProtectionAsync();

                if (!result.IsSuccessful)
                {
                    SetLocalizedStatusMessage(
                        "RecoverySystemProtectionError",
                        result.Message);

                    return;
                }

                SetLocalizedStatusMessage(
                    "RecoverySystemProtectionEnabled");

                IsSystemProtectionActionRequired =
                    false;

                await Task.Delay(
                    TimeSpan.FromSeconds(2));

                await CheckAvailabilityAsync();
            }
            catch (Exception ex)
            {
                SetLocalizedStatusMessage(
                    "RecoverySystemProtectionError",
                    ex.Message);
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
                SetLocalizedStatusMessage(
                    "RecoveryNoRestorePointSelected");

                return;
            }

            string confirmationMessage =
           LocalizationHelper.Format(
               "RecoveryRestoreDialogMessage",
               selectedRestorePoint.CreatedAtDisplay,
               selectedRestorePoint.Description);

            string confirmationTitle =
                LocalizationHelper.Get(
                    "RecoveryRestoreDialogTitle");

            bool confirmation =
      NativeConfirmationDialog.Ask(
          Application.Current.MainWindow,
          confirmationTitle,
          confirmationMessage,
          LocalizationHelper.Get(
              "RecoveryRestoreConfirmYes"),
          LocalizationHelper.Get(
              "RecoveryRestoreConfirmNo"));

            if (!confirmation)
            {
                SetLocalizedStatusMessage(
                    "RecoveryRestoreCancelled");

                return;
            }
            IsRestoringSystem =
                true;

            SetLocalizedStatusMessage(
                "RecoveryStartingRestore");

            try
            {
                string workerPath =
                    Path.Combine(
                        AppContext.BaseDirectory,
                        "WinBoost.RecoveryWorker.exe");

                if (!File.Exists(
                        workerPath))
                {
                    IsRestartRequired =
                        false;

                    SetLocalizedStatusMessage(
                        "RecoveryRestoreError",
                        $"Recovery worker not found: {workerPath}");

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

                startInfo.ArgumentList.Add(
                    "--restore");

                startInfo.ArgumentList.Add(
                    $"--sequence-number={selectedRestorePoint.SequenceNumber}");

                Process? workerProcess =
                    Process.Start(
                        startInfo);

                if (workerProcess == null)
                {
                    IsRestartRequired =
                        false;

                    SetLocalizedStatusMessage(
                        "RecoveryRestoreError",
                        "WinBoost Recovery Worker could not be started.");

                    return;
                }

                await workerProcess
                    .WaitForExitAsync();

                if (workerProcess.ExitCode != 0)
                {
                    IsRestartRequired =
                        false;

                    SetLocalizedStatusMessage(
                        "RecoveryRestoreFailed",
                        $"Recovery Worker exit code: " +
                        $"{workerProcess.ExitCode}");

                    return;
                }

                await SavePendingRestoreAsync(
                    selectedRestorePoint);

                /*
                 * Windows a acceptat restaurarea.
                 * Calculatorul NU este repornit automat aici.
                 */

                IsRestartRequired =
                    true;

                SetLocalizedStatusMessage(
                    "RecoveryRestorePrepared");
            }
            catch (System.ComponentModel.Win32Exception ex)
                when (ex.NativeErrorCode == 1223)
            {
                IsRestartRequired =
                    false;

                SetLocalizedStatusMessage(
                    "RecoveryRestoreCancelled");
            }
            catch (Exception ex)
            {
                IsRestartRequired =
                    false;

                SetLocalizedStatusMessage(
                    "RecoveryRestoreError",
                    ex.Message);
            }
            finally
            {
                IsRestoringSystem =
                    false;
            }
        }


        // ======================================
        // RESTART NOW
        // ======================================

        private Task RestartNowAsync()
        {
            if (!CanRestartNow)
            {
                return Task.CompletedTask;
            }

            string confirmationMessage =
                LocalizationHelper.Get(
                    "RecoveryRestartDialogMessage");

            string confirmationTitle =
                LocalizationHelper.Get(
                    "RecoveryRestartDialogTitle");

            MessageBoxResult confirmation =
                MessageBox.Show(
                    confirmationMessage,
                    confirmationTitle,
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

            if (confirmation !=
                MessageBoxResult.Yes)
            {
                SetLocalizedStatusMessage(
                    "RecoveryRestartCancelled");

                return Task.CompletedTask;
            }

            try
            {
                SetLocalizedStatusMessage(
                    "RecoveryRestarting");

                var startInfo =
                    new ProcessStartInfo
                    {
                        FileName =
                            "shutdown.exe",

                        Arguments =
                            "/r /t 0",

                        UseShellExecute =
                            false,

                        CreateNoWindow =
                            true
                    };

                Process.Start(
                    startInfo);
            }
            catch (Exception ex)
            {
                SetLocalizedStatusMessage(
                    "RecoveryWindowsRestartError",
                    ex.Message);
            }

            return Task.CompletedTask;
        }


        // ======================================
        // RESTART LATER
        // ======================================

        private Task RestartLaterAsync()
        {
            if (!CanRestartLater)
            {
                return Task.CompletedTask;
            }

            /*
             * Nu schimbăm IsRestartRequired în false.
             *
             * Restaurarea rămâne pregătită, iar zona
             * Restart required trebuie să rămână vizibilă.
             */

            SetLocalizedStatusMessage(
                "RecoveryRestartLaterMessage");

            return Task.CompletedTask;
        }

        // ======================================
        // RECOVERY WORKER DATA
        // ======================================

        private static string GetRestorePointTypeName(
            uint restorePointType)
        {
            return restorePointType switch
            {
                0 =>
                    "Application Install",

                1 =>
                    "Application Uninstall",

                10 =>
                    "Device Driver Install",

                12 =>
                    "System",

                13 =>
                    "Cancelled Operation",

                _ =>
                    "Other"
            };
        }

        private sealed class RestorePointWorkerItem
        {
            public uint SequenceNumber
            {
                get;
                init;
            }

            public string Description
            {
                get;
                init;
            } = string.Empty;

            public uint RestorePointType
            {
                get;
                init;
            }

            public DateTime CreatedAt
            {
                get;
                init;
            }
        }

        // ======================================
        // SYSTEM PROTECTION ERROR DETECTION
        // ======================================

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

        public async Task LoadCachedRestorePointsAsync()
        {
            try
            {
                if (!File.Exists(
                        RestorePointsCachePath))
                {
                    RestorePoints.Clear();

                    SelectedRestorePoint =
                        null;

                    RestorePointsMessage =
                        LocalizationHelper.Get(
                            "RecoveryNoCachedRestorePoints");

                    return;
                }

                string json =
                    await File.ReadAllTextAsync(
                        RestorePointsCachePath);

                List<RestorePointWorkerItem>?
                    restorePoints =
                        JsonSerializer.Deserialize<
                            List<RestorePointWorkerItem>>(
                                json);

                RestorePoints.Clear();

                if (restorePoints != null)
                {
                    foreach (RestorePointWorkerItem
                             restorePoint in restorePoints)
                    {
                        RestorePoints.Add(
                            new SystemRestorePointInfo
                            {
                                SequenceNumber =
                                    restorePoint.SequenceNumber,

                                Description =
                                    restorePoint.Description,

                                RestorePointType =
                                    restorePoint.RestorePointType,

                                RestorePointTypeName =
                                    GetRestorePointTypeName(
                                        restorePoint.RestorePointType),

                                CreatedAt =
                                    restorePoint.CreatedAt
                            });
                    }
                }

                SelectedRestorePoint =
                    RestorePoints.Count > 0
                        ? RestorePoints[0]
                        : null;

                RestorePointsMessage =
                    RestorePoints.Count == 0
                        ? LocalizationHelper.Get(
                            "RecoveryNoRestorePoints")
                        : LocalizationHelper.Format(
                            "RecoveryRestorePointsFound",
                            RestorePoints.Count);
            }
            catch
            {
                RestorePoints.Clear();

                SelectedRestorePoint =
                    null;

                RestorePointsMessage =
                    LocalizationHelper.Get(
                        "RecoveryNoCachedRestorePoints");
            }
        }

        private static async Task SaveRestorePointsCacheAsync(
            List<RestorePointWorkerItem> restorePoints)
        {
            string? directory =
                Path.GetDirectoryName(
                    RestorePointsCachePath);

            if (!string.IsNullOrWhiteSpace(
                    directory))
            {
                Directory.CreateDirectory(
                    directory);
            }

            string json =
                JsonSerializer.Serialize(
                    restorePoints,
                    new JsonSerializerOptions
                    {
                        WriteIndented =
                            true
                    });

            await File.WriteAllTextAsync(
                RestorePointsCachePath,
                json);
        }

        private sealed class PendingRestoreInfo
        {
            public uint SequenceNumber
            {
                get;
                init;
            }

            public string Description
            {
                get;
                init;
            } = string.Empty;

            public DateTime RestorePointCreatedAt
            {
                get;
                init;
            }

            public DateTime RequestedAt
            {
                get;
                init;
            }
        }

        private static async Task SavePendingRestoreAsync(
    SystemRestorePointInfo restorePoint)
        {
            string? directory =
                Path.GetDirectoryName(
                    PendingRestorePath);

            if (!string.IsNullOrWhiteSpace(
                    directory))
            {
                Directory.CreateDirectory(
                    directory);
            }

            var pendingRestore =
                new PendingRestoreInfo
                {
                    SequenceNumber =
                        restorePoint.SequenceNumber,

                    Description =
                        restorePoint.Description,

                    RestorePointCreatedAt =
                        restorePoint.CreatedAt,

                    RequestedAt =
                        DateTime.Now
                };

            string json =
                JsonSerializer.Serialize(
                    pendingRestore,
                    new JsonSerializerOptions
                    {
                        WriteIndented =
                            true
                    });

            await File.WriteAllTextAsync(
                PendingRestorePath,
                json);
        }

        private static async Task<DateTime?>
    GetLastSuccessfulSystemRestoreAsync()
        {
            var startInfo =
                new ProcessStartInfo
                {
                    FileName =
                        "wevtutil.exe",

                    Arguments =
                        "qe Application " +
                        "/q:\"*[System[(EventID=8202)]]\" " +
                        "/rd:true /c:1 /f:xml",

                    UseShellExecute =
                        false,

                    RedirectStandardOutput =
                        true,

                    RedirectStandardError =
                        true,

                    CreateNoWindow =
                        true
                };

            using Process? process =
                Process.Start(
                    startInfo);

            if (process == null)
            {
                return null;
            }

            string output =
                await process.StandardOutput
                    .ReadToEndAsync();

            await process.WaitForExitAsync();

            if (process.ExitCode != 0 ||
                string.IsNullOrWhiteSpace(output))
            {
                return null;
            }

            try
            {
                XDocument document =
                    XDocument.Parse(
                        output);

                XNamespace ns =
                    "http://schemas.microsoft.com/win/2004/08/events/event";

                XElement? timeCreated =
                    document
                        .Descendants(
                            ns + "TimeCreated")
                        .FirstOrDefault();

                string? systemTime =
                    timeCreated?
                        .Attribute(
                            "SystemTime")?
                        .Value;

                if (DateTime.TryParse(
                        systemTime,
                        null,
                        System.Globalization.DateTimeStyles
                            .RoundtripKind,
                        out DateTime eventTime))
                {
                    return eventTime
                        .ToLocalTime();
                }
            }
            catch
            {
                // Dacă jurnalul nu poate fi interpretat,
                // nu raportăm fals succes.
            }

            return null;
        }

        public async Task CheckPendingRestoreResultAsync()
        {
            if (!File.Exists(
                    PendingRestorePath))
            {
                return;
            }

            try
            {
                string json =
                    await File.ReadAllTextAsync(
                        PendingRestorePath);

                PendingRestoreInfo?
                    pendingRestore =
                        JsonSerializer.Deserialize<
                            PendingRestoreInfo>(
                                json);

                if (pendingRestore == null)
                {
                    return;
                }

                DateTime? successfulRestoreAt =
                    await GetLastSuccessfulSystemRestoreAsync();

                if (!successfulRestoreAt.HasValue)
                {
                    return;
                }

                /*
                 * Evenimentul de succes trebuie să fie ulterior
                 * momentului în care WinBoost a cerut restaurarea.
                 *
                 * Tolerăm un minut pentru diferențe foarte mici
                 * de timp între procese / jurnalizare.
                 */

                if (successfulRestoreAt.Value <
                    pendingRestore.RequestedAt
                        .AddMinutes(-1))
                {
                    return;
                }

                SetLocalizedStatusMessage(
                    "RecoveryRestoreCompletedSuccessfully",
                    pendingRestore
                        .RestorePointCreatedAt
                        .ToString(
                            "dd.MM.yyyy HH:mm:ss"));

                try
                {
                    File.Delete(
                        PendingRestorePath);
                }
                catch
                {
                    /*
                     * Mesajul de succes rămâne valid chiar dacă
                     * fișierul temporar nu poate fi șters.
                     */
                }
            }
            catch
            {
                /*
                 * Nu raportăm succes sau eșec dacă starea
                 * persistentă nu poate fi verificată.
                 */
            }
        }

        private void SetLocalizedStatusMessage(
            string resourceKey,
            params object[] arguments)
        {
            _statusMessageResourceKey =
                resourceKey;

            _statusMessageArguments =
                arguments ?? Array.Empty<object>();

            RefreshLocalizedStatusMessage();
        }

        private void ClearStatusMessage()
        {
            _statusMessageResourceKey =
                null;

            _statusMessageArguments =
                Array.Empty<object>();

            StatusMessage =
                string.Empty;
        }

        private void RefreshLocalizedStatusMessage()
        {
            if (string.IsNullOrWhiteSpace(
                    _statusMessageResourceKey))
            {
                return;
            }

            StatusMessage =
                _statusMessageArguments.Length == 0
                    ? LocalizationHelper.Get(
                        _statusMessageResourceKey)
                    : LocalizationHelper.Format(
                        _statusMessageResourceKey,
                        _statusMessageArguments);
        }


        // ======================================
        // COMMAND STATES
        // ======================================

        private async void OnLanguageChanged(
            object? sender,
              EventArgs e)
        {
            /*
             * Refacem mesajele dinamice atunci când
             * utilizatorul schimbă limba RO / EN.
             */

            if (IsSystemRestoreAvailable)
            {
                AvailabilityMessage =
                    LocalizationHelper.Get(
                        "RecoverySystemRestoreAvailable");
            }
            else
            {
                AvailabilityMessage =
                    LocalizationHelper.Get(
                        "RecoverySystemRestoreUnavailable");
            }

            if (!IsLoadingRestorePoints)
            {
                RestorePointsMessage =
                    RestorePoints.Count == 0
                        ? LocalizationHelper.Get(
                            "RecoveryNoRestorePoints")
                        : LocalizationHelper.Format(
                            "RecoveryRestorePointsFound",
                            RestorePoints.Count);
            }

            RefreshLocalizedStatusMessage();

            /*
             * Reîncărcăm punctele de restaurare pentru ca
             * DisplayDescription și DisplayRestorePointTypeName
             * să fie recalculate în limba nou selectată.
             */

            if (!IsLoadingRestorePoints)
            {
                await LoadCachedRestorePointsAsync();
            }
        }
        private void NotifyCommandStates()
        {
            OnPropertyChanged(
                nameof(CanCreateRestorePoint));

            OnPropertyChanged(
                nameof(CanEnableSystemProtection));

            OnPropertyChanged(
                nameof(CanRestoreSystem));

            OnPropertyChanged(
                nameof(CanRestartNow));

            OnPropertyChanged(
                nameof(CanRestartLater));

            _createRestorePointCommand
                .RaiseCanExecuteChanged();

            _refreshRestorePointsCommand
                .RaiseCanExecuteChanged();

            _enableSystemProtectionCommand
                .RaiseCanExecuteChanged();

            _restoreSystemCommand
                .RaiseCanExecuteChanged();

            _restartNowCommand
                .RaiseCanExecuteChanged();

            _restartLaterCommand
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