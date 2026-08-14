using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace WinBoost.UpdateWorker
{
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.AutoDispatch)]
    public sealed class InstallationProgressCallback
    {
        [DispId(0)]
        public void Invoke(
            object installationJob,
            object callbackArgs)
        {
        }
    }

    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.AutoDispatch)]
    public sealed class InstallationCompletedCallback
    {
        [DispId(0)]
        public void Invoke(
            object installationJob,
            object callbackArgs)
        {
        }
    }

    internal class Program
    {
        private static readonly TimeSpan
            InstallationTimeout =
                TimeSpan.FromMinutes(30);

        private static readonly TimeSpan
            AbortWaitTimeout =
                TimeSpan.FromMinutes(2);

        private static int Main(string[] args)
        {
            try
            {
                var selectedUpdateIds =
                    ParseSelectedUpdateIds(args);

                string language =
                  ParseLanguage(args);

                if (selectedUpdateIds.Count == 0)
                {
                    return Fail(
                        9,
                        language,
                        "No Windows updates were selected for installation.",
                        "Nu a fost selectată nicio actualizare Windows pentru instalare.");
                }

                UpdateWorkerStatusWriter.Reset();

                WriteStatus(
         state: "Searching",
         percent: 0,
         message:
             Localize(
                 language,
                 "Searching for available Windows updates...",
                 "Se caută actualizări Windows disponibile..."));

                Console.WriteLine(
                    "WinBoost Update Worker");

                Console.WriteLine(
                    "Searching for available Windows updates...");

                Type? sessionType =
                    Type.GetTypeFromProgID(
                        "Microsoft.Update.Session");
                if (sessionType == null)
                {
                    return Fail(
                        10,
                        language,
                        "Windows Update Agent is not available.",
                        "Agentul Windows Update nu este disponibil.");
                }

                dynamic? session =
                    Activator.CreateInstance(
                        sessionType);

                if (session == null)
                {
                    return Fail(
                        11,
                        language,
                        "Windows Update session could not be created.",
                        "Sesiunea Windows Update nu a putut fi creată.");
                }

                session.ClientApplicationID =
                    "WinBoost Pro 11";

                dynamic searcher =
                    session.CreateUpdateSearcher();

                dynamic searchResult =
                    searcher.Search(
                        "IsInstalled=0 and IsHidden=0");

                int updateCount =
                    searchResult.Updates.Count;

                Console.WriteLine(
                    $"Available updates: {updateCount}");

                if (updateCount == 0)
                {
                    WriteStatus(
                        state: "Completed",
                        percent: 100,
                       message:
                               Localize(
                                      language,
                                      "No updates are available.",
                                      "Nu sunt disponibile actualizări."),
                        isCompleted: true,
                        isSuccessful: true);

                    return 0;
                }

                Type? collectionType =
                    Type.GetTypeFromProgID(
                        "Microsoft.Update.UpdateColl");

                if (collectionType == null)
                {
                    return Fail(
                        12,
                        language,
                        "Windows Update collection could not be created.",
                        "Colecția Windows Update nu a putut fi creată.");
                }

                dynamic? updatesToInstall =
                    Activator.CreateInstance(
                        collectionType);

                if (updatesToInstall == null)
                {
                    return Fail(
                        13,
                        language,
                        "Windows Update collection could not be created.",
                        "Colecția Windows Update nu a putut fi creată.");
                }

                for (int index = 0;
                     index < updateCount;
                     index++)
                {
                    dynamic update =
                        searchResult.Updates.Item(index);

                    string updateId =
                        Convert.ToString(
                            update.Identity.UpdateID)
                        ?? string.Empty;

                    if (!selectedUpdateIds.Contains(
                            updateId))
                    {
                        continue;
                    }

                    string title =
                        Convert.ToString(
                            update.Title)
                        ?? "Unknown update";

                    Console.WriteLine(
                        $"Preparing: {title}");

                    try
                    {
                        if (!update.EulaAccepted)
                        {
                            update.AcceptEula();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine(
                            $"EULA could not be accepted for: {title}");

                        Console.Error.WriteLine(
                            ex.Message);

                        continue;
                    }

                    try
                    {
                        updatesToInstall.Add(
                            update);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine(
                            $"Update could not be added: {title}");

                        Console.Error.WriteLine(
                            ex.Message);
                    }
                }

                int preparedUpdateCount =
                    Convert.ToInt32(
                        updatesToInstall.Count);

                if (preparedUpdateCount == 0)
                {
                    return Fail(
                        14,
                        language,
                        "No updates could be prepared for installation.",
                        "Nicio actualizare nu a putut fi pregătită pentru instalare.");
                }

                Console.WriteLine();

                Console.WriteLine(
                    $"Downloading {preparedUpdateCount} update(s)...");

                WriteStatus(
                      state: "Downloading",
                         percent: 0,
                        totalUpdates: preparedUpdateCount,
                         message:
                            Localize(
                                language,
                              $"Downloading {preparedUpdateCount} update(s)...",
                               $"Se descarcă {preparedUpdateCount} actualizări..."));
                dynamic downloader =
                    session.CreateUpdateDownloader();

                downloader.Updates =
                    updatesToInstall;

                object? downloadResult =
                   RunDownloadWithActivityIndicator(
                           downloader,
                           preparedUpdateCount,
                           language);

                if (downloadResult == null)
                {
                    return Fail(
                        19,
                        language,
                        "Download did not return a result.",
                        "Descărcarea nu a returnat niciun rezultat.");
                }

                dynamic dynamicDownloadResult =
                    downloadResult;

                Console.WriteLine();

                Console.WriteLine(
                    $"Download result code: " +
                    $"{dynamicDownloadResult.ResultCode}");

                Type? installCollectionType =
                    Type.GetTypeFromProgID(
                        "Microsoft.Update.UpdateColl");

                if (installCollectionType == null)
                {
                    return Fail(
                        15,
                        language,
                        "Installation collection could not be created.",
                        "Colecția pentru instalare nu a putut fi creată.");
                }

                dynamic? downloadedUpdates =
                    Activator.CreateInstance(
                        installCollectionType);

                if (downloadedUpdates == null)
                {
                    return Fail(
                        16,
                        language,
                        "Installation collection could not be created.",
                        "Colecția pentru instalare nu a putut fi creată.");
                }

                for (int index = 0;
                     index < preparedUpdateCount;
                     index++)
                {
                    dynamic update =
                        updatesToInstall.Item(index);

                    string title =
                        Convert.ToString(
                            update.Title)
                        ?? "Unknown update";

                    if (update.IsDownloaded)
                    {
                        downloadedUpdates.Add(
                            update);

                        Console.WriteLine(
                            $"Downloaded: {title}");
                    }
                    else
                    {
                        Console.WriteLine(
                            $"Not downloaded: {title}");
                    }
                }

                int downloadedUpdateCount =
                    Convert.ToInt32(
                        downloadedUpdates.Count);

                if (downloadedUpdateCount == 0)
                {
                    return Fail(
                        20,
                        language,
                        "No updates were downloaded successfully.",
                        "Nicio actualizare nu a fost descărcată cu succes.");
                }

                Console.WriteLine();

                Console.WriteLine(
                    $"Installing {downloadedUpdateCount} update(s)...");

                WriteStatus(
                    state: "Installing",
                    percent: 0,
                    currentUpdate: 1,
                    totalUpdates: downloadedUpdateCount,
                    message:
                        Localize(
                            language,
                            $"Installing {downloadedUpdateCount} update(s)...",
                            $"Se instalează {downloadedUpdateCount} actualizări..."));

                dynamic installer =
                    session.CreateUpdateInstaller();

                installer.ClientApplicationID =
                    "WinBoost Pro 11";

                installer.Updates =
                    downloadedUpdates;

                if (installer.IsBusy)
                {
                    return Fail(
                        21,
                        language,
                        "Windows Update is already installing or removing updates.",
                        "Windows Update instalează sau elimină deja actualizări.");
                }

                if (installer.RebootRequiredBeforeInstallation)
                {
                    return Fail(
                        22,
                        language,
                        "Windows requires a restart before updates can be installed.",
                        "Windows necesită o repornire înainte ca actualizările să poată fi instalate.");
                }

                object? installationResult =
                   RunInstallationWithProgress(
                        installer,
                      downloadedUpdates,
                         language);

                if (installationResult == null)
                {
                    return Fail(
                        23,
                        language,
                        "Windows Update installation did not complete.",
                        "Instalarea Windows Update nu s-a finalizat.");
                }

                dynamic dynamicInstallationResult =
                    installationResult;

                bool rebootRequired =
                    Convert.ToBoolean(
                        dynamicInstallationResult.RebootRequired);

                int installationResultCode =
                    Convert.ToInt32(
                        dynamicInstallationResult.ResultCode);

                Console.WriteLine();

                Console.WriteLine(
                    $"Installation result code: " +
                    $"{installationResultCode}");

                Console.WriteLine(
                    $"Restart required: " +
                    $"{rebootRequired}");

                for (int index = 0;
                     index < downloadedUpdateCount;
                     index++)
                {
                    dynamic update =
                        downloadedUpdates.Item(index);

                    dynamic updateResult =
                        dynamicInstallationResult
                            .GetUpdateResult(index);

                    string title =
                        Convert.ToString(
                            update.Title)
                        ?? "Unknown update";

                    Console.WriteLine();

                    Console.WriteLine(
                        title);

                    Console.WriteLine(
                        $"Result: {updateResult.ResultCode}");

                    Console.WriteLine(
                        $"HRESULT: 0x" +
                        $"{unchecked((uint)updateResult.HResult):X8}");

                    Console.WriteLine(
                        $"Restart required: " +
                        $"{updateResult.RebootRequired}");
                }

                bool installationSucceeded =
                    installationResultCode == 2 ||
                    installationResultCode == 3;

                string completedMessage;

                if (rebootRequired)
                {
                    completedMessage =
                        Localize(
                            language,
                            "Windows updates were installed. A restart is required.",
                            "Actualizările Windows au fost instalate. Este necesară o repornire.");
                }
                else if (installationSucceeded)
                {
                    completedMessage =
                        Localize(
                            language,
                            "Windows updates were installed successfully.",
                            "Actualizările Windows au fost instalate cu succes.");
                }
                else
                {
                    completedMessage =
                        Localize(
                            language,
                            "Windows Update completed with warnings.",
                            "Windows Update s-a finalizat cu avertismente.");
                }

                WriteStatus(
                    state: "Completed",
                    percent: 100,
                    currentUpdate: downloadedUpdateCount,
                    totalUpdates: downloadedUpdateCount,
                    message: completedMessage,
                    rebootRequired: rebootRequired,
                    isCompleted: true,
                    isSuccessful: installationSucceeded);

                Console.WriteLine();

                Console.WriteLine(
                    "Windows Update operation completed.");

                return installationSucceeded
                    ? 0
                    : 24;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine();

                Console.Error.WriteLine(
                    "WinBoost Update Worker failed.");

                Console.Error.WriteLine(
                    ex.Message);

                Console.Error.WriteLine(
                    ex);

                WriteStatus(
                    state: "Error",
                    percent: 0,
                    message:
                        Localize(
                            ParseLanguage(args),
                            "Windows Update operation failed.",
                            "Operația Windows Update a eșuat."),
                    isCompleted: true,
                    isSuccessful: false,
                    errorMessage: ex.Message);

                return 100;
            }
        }

        private static HashSet<string>
            ParseSelectedUpdateIds(
                string[] args)
        {
            var selectedUpdateIds =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);

            foreach (string argument in args)
            {
                if (string.IsNullOrWhiteSpace(
                        argument))
                {
                    continue;
                }

                string value =
                    argument.Trim();

                const string prefix =
                    "--update-id=";

                if (value.StartsWith(
                        prefix,
                        StringComparison.OrdinalIgnoreCase))
                {
                    value =
                        value.Substring(
                            prefix.Length);
                }

                value =
                    value.Trim(
                        '"');

                if (!string.IsNullOrWhiteSpace(
                        value))
                {
                    selectedUpdateIds.Add(
                        value);
                }
            }

            return selectedUpdateIds;
        }

        private static string ParseLanguage(
           string[] args)
        {
            const string prefix =
                "--language=";

            foreach (string argument in args)
            {
                if (string.IsNullOrWhiteSpace(
                        argument))
                {
                    continue;
                }

                if (!argument.StartsWith(
                        prefix,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string language =
                    argument.Substring(
                        prefix.Length)
                    .Trim()
                    .ToLowerInvariant();

                if (language == "ro" ||
                    language == "en")
                {
                    return language;
                }
            }

            return "en";
        }

        private static string Localize(
               string language,
               string english,
               string romanian)
        {
            return string.Equals(
                language,
                "ro",
                StringComparison.OrdinalIgnoreCase)
                    ? romanian
                    : english;
        }
        private static object? RunDownloadWithActivityIndicator(
       dynamic downloader,
       int totalUpdates,
       string language)
        {
            object? downloadResult =
                null;

            Exception? downloadException =
                null;

            bool completed =
                false;

            DateTime startedAt =
                DateTime.Now;

            Thread downloadThread =
                new Thread(
                    () =>
                    {
                        try
                        {
                            downloadResult =
                                downloader.Download();
                        }
                        catch (Exception ex)
                        {
                            downloadException =
                                ex;
                        }
                        finally
                        {
                            completed =
                                true;
                        }
                    });

            downloadThread.SetApartmentState(
                ApartmentState.STA);

            downloadThread.IsBackground =
                false;

            downloadThread.Start();

            while (!completed)
            {
                Thread.Sleep(
                    TimeSpan.FromSeconds(5));

                if (completed)
                {
                    break;
                }

                TimeSpan elapsed =
                    DateTime.Now -
                    startedAt;

                string message =
                      Localize(
                        language,
                             $"Downloading Windows updates... {elapsed:mm\\:ss}",
                           $"Se descarcă actualizările Windows... {elapsed:mm\\:ss}");
                Console.WriteLine(
                    $"Download in progress... elapsed " +
                    $"{elapsed:mm\\:ss}");

                WriteStatus(
                    state: "Downloading",
                    percent: 0,
                    totalUpdates: totalUpdates,
                    message: message);
            }

            downloadThread.Join();

            if (downloadException != null)
            {
                throw downloadException;
            }

            return downloadResult;
        }

        private static object? RunInstallationWithProgress(
             dynamic installer,
             dynamic downloadedUpdates,
             string language)
        {
            var progressCallback =
                new InstallationProgressCallback();

            var completedCallback =
                new InstallationCompletedCallback();

            dynamic? installationJob =
                null;

            try
            {
                installationJob =
                    installer.BeginInstall(
                        progressCallback,
                        completedCallback,
                        null);

                DateTime startedAt =
                    DateTime.Now;

                int lastOverallPercent =
                    -1;

                int lastUpdateIndex =
                    -1;

                int lastUpdatePercent =
                    -1;

                DateTime lastActivityMessage =
                    DateTime.MinValue;

                int totalUpdates =
                    Convert.ToInt32(
                        downloadedUpdates.Count);

                while (!(bool)installationJob.IsCompleted)
                {
                    try
                    {
                        dynamic progress =
                            installationJob.GetProgress();

                        int overallPercent =
                            Convert.ToInt32(
                                progress.PercentComplete);

                        int currentUpdateIndex =
                            Convert.ToInt32(
                                progress.CurrentUpdateIndex);

                        int currentUpdatePercent =
                            Convert.ToInt32(
                                progress.CurrentUpdatePercentComplete);

                        bool changed =
                            overallPercent != lastOverallPercent ||
                            currentUpdateIndex != lastUpdateIndex ||
                            currentUpdatePercent != lastUpdatePercent;

                        bool activityMessageDue =
                            DateTime.Now -
                            lastActivityMessage >=
                            TimeSpan.FromSeconds(10);

                        if (changed ||
                            activityMessageDue)
                        {
                            string currentTitle =
                                GetUpdateTitle(
                                    downloadedUpdates,
                                    currentUpdateIndex);

                            TimeSpan elapsed =
                                DateTime.Now -
                                startedAt;

                            Console.WriteLine(
                                $"Installing: {overallPercent}% overall | " +
                                $"Update {currentUpdateIndex + 1}/" +
                                $"{totalUpdates} | " +
                                $"{currentUpdatePercent}% | " +
                                $"{elapsed:mm\\:ss}");

                            if (!string.IsNullOrWhiteSpace(
                                currentTitle))
                            {
                                Console.WriteLine(
                                    $"Current: {currentTitle}");
                            }

                            WriteStatus(
                                state: "Installing",
                                percent: overallPercent,
                                currentUpdate:
                                    currentUpdateIndex + 1,
                                totalUpdates: totalUpdates,
                                currentUpdateTitle:
                                    currentTitle,
                                message:
                                    Localize(
                                        language,
                                        $"Installing update {currentUpdateIndex + 1} of {totalUpdates}...",
                                        $"Se instalează actualizarea {currentUpdateIndex + 1} din {totalUpdates}..."));

                            lastOverallPercent =
                                overallPercent;

                            lastUpdateIndex =
                                currentUpdateIndex;

                            lastUpdatePercent =
                                currentUpdatePercent;

                            lastActivityMessage =
                                DateTime.Now;
                        }
                    }
                    catch (Exception ex)
                    {
                        TimeSpan elapsed =
                            DateTime.Now -
                            startedAt;

                        Console.WriteLine(
                            $"Installation still running... " +
                            $"elapsed {elapsed:mm\\:ss}");

                        Console.WriteLine(
                            $"Progress temporarily unavailable: " +
                            $"{ex.Message}");

                        WriteStatus(
                            state: "Installing",
                            percent:
                                Math.Max(
                                    lastOverallPercent,
                                    0),
                            currentUpdate:
                                Math.Max(
                                    lastUpdateIndex + 1,
                                    1),
                            totalUpdates: totalUpdates,
                            message:
                                Localize(
                                    language,
                                    $"Installation is still running... {elapsed:mm\\:ss}",
                                    $"Instalarea este încă în desfășurare... {elapsed:mm\\:ss}"));
                    }

                    TimeSpan totalElapsed =
                        DateTime.Now -
                        startedAt;

                    if (totalElapsed >=
                        InstallationTimeout)
                    {
                        Console.Error.WriteLine();

                        Console.Error.WriteLine(
                            "Installation exceeded the WinBoost safety timeout.");

                        Console.Error.WriteLine(
                            "Requesting Windows Update to stop the operation...");

                        WriteStatus(
                            state: "Error",
                            percent:
                                Math.Max(
                                    lastOverallPercent,
                                    0),
                            currentUpdate:
                                Math.Max(
                                    lastUpdateIndex + 1,
                                    1),
                            totalUpdates: totalUpdates,
                            message:
                                Localize(
                                    language,
                                    "Windows Update installation exceeded the safety timeout.",
                                    "Instalarea Windows Update a depășit limita de siguranță."),
                            isCompleted: false,
                            isSuccessful: false);

                        try
                        {
                            installationJob.RequestAbort();
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine(
                                $"Abort request failed: {ex.Message}");
                        }

                        DateTime abortStarted =
                            DateTime.Now;

                        while (!(bool)installationJob.IsCompleted &&
                               DateTime.Now - abortStarted <
                               AbortWaitTimeout)
                        {
                            Thread.Sleep(
                                TimeSpan.FromSeconds(2));
                        }

                        if (!(bool)installationJob.IsCompleted)
                        {
                            Console.Error.WriteLine(
                                "Windows Update did not stop within the safety period.");

                            return null;
                        }

                        break;
                    }

                    Thread.Sleep(
                        TimeSpan.FromSeconds(2));
                }

                dynamic installationResult =
                    installer.EndInstall(
                        installationJob);

                return installationResult;
            }
            finally
            {
                if (installationJob != null)
                {
                    try
                    {
                        if ((bool)installationJob.IsCompleted)
                        {
                            installationJob.CleanUp();
                        }
                    }
                    catch
                    {
                    }
                }

                GC.KeepAlive(
                    progressCallback);

                GC.KeepAlive(
                    completedCallback);
            }
        }

        private static string GetUpdateTitle(
            dynamic updates,
            int index)
        {
            try
            {
                if (index < 0 ||
                    index >= updates.Count)
                {
                    return string.Empty;
                }

                dynamic update =
                    updates.Item(index);

                return Convert.ToString(
                    update.Title)
                    ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static int Fail(
            int exitCode,
            string language,
            string englishMessage,
            string romanianMessage)
        {
            string message =
                Localize(
                    language,
                    englishMessage,
                    romanianMessage);

            Console.Error.WriteLine(
                message);

            WriteStatus(
                state: "Error",
                percent: 0,
                message: message,
                isCompleted: true,
                isSuccessful: false,
                errorMessage: message);

            return exitCode;
        }

        private static void WriteStatus(
            string state,
            int percent,
            string message,
            int currentUpdate = 0,
            int totalUpdates = 0,
            string currentUpdateTitle = "",
            bool rebootRequired = false,
            bool isCompleted = false,
            bool isSuccessful = false,
            string errorMessage = "")
        {
            UpdateWorkerStatusWriter.Write(
                new UpdateWorkerStatus
                {
                    State = state,
                    Percent =
                        Math.Clamp(
                            percent,
                            0,
                            100),
                    CurrentUpdate =
                        currentUpdate,
                    TotalUpdates =
                        totalUpdates,
                    CurrentUpdateTitle =
                        currentUpdateTitle,
                    Message =
                        message,
                    RebootRequired =
                        rebootRequired,
                    IsCompleted =
                        isCompleted,
                    IsSuccessful =
                        isSuccessful,
                    ErrorMessage =
                        errorMessage
                });
        }
    }
}