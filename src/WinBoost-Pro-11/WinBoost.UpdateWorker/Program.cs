using System;
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
            // Progresul este citit controlat din IInstallationJob.GetProgress().
            // Callback-ul există pentru contractul BeginInstall.
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
            // Finalizarea este verificată prin IInstallationJob.IsCompleted.
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
                Console.WriteLine(
                    "WinBoost Update Worker");

                Console.WriteLine(
                    "Searching for available Windows updates...");

                Type? sessionType =
                    Type.GetTypeFromProgID(
                        "Microsoft.Update.Session");

                if (sessionType == null)
                {
                    Console.Error.WriteLine(
                        "Windows Update Agent is not available.");

                    return 10;
                }

                dynamic? session =
                    Activator.CreateInstance(
                        sessionType);

                if (session == null)
                {
                    Console.Error.WriteLine(
                        "Windows Update session could not be created.");

                    return 11;
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
                    Console.WriteLine(
                        "No updates are available.");

                    return 0;
                }

                Type? collectionType =
                    Type.GetTypeFromProgID(
                        "Microsoft.Update.UpdateColl");

                if (collectionType == null)
                {
                    Console.Error.WriteLine(
                        "Windows Update collection could not be created.");

                    return 12;
                }

                dynamic? updatesToInstall =
                    Activator.CreateInstance(
                        collectionType);

                if (updatesToInstall == null)
                {
                    Console.Error.WriteLine(
                        "Windows Update collection could not be created.");

                    return 13;
                }

                for (int index = 0;
                     index < updateCount;
                     index++)
                {
                    dynamic update =
                        searchResult.Updates.Item(index);

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

                if (updatesToInstall.Count == 0)
                {
                    Console.Error.WriteLine(
                        "No updates could be prepared for installation.");

                    return 14;
                }

                Console.WriteLine();

                Console.WriteLine(
                    $"Downloading {updatesToInstall.Count} update(s)...");

                dynamic downloader =
                    session.CreateUpdateDownloader();

                downloader.Updates =
                    updatesToInstall;

                object? downloadResult =
                    RunDownloadWithActivityIndicator(
                        downloader);

                if (downloadResult == null)
                {
                    Console.Error.WriteLine(
                        "Download did not return a result.");

                    return 19;
                }

                dynamic dynamicDownloadResult =
                    downloadResult;

                Console.WriteLine();

                Console.WriteLine(
                    $"Download result code: {dynamicDownloadResult.ResultCode}");

                Type? installCollectionType =
                    Type.GetTypeFromProgID(
                        "Microsoft.Update.UpdateColl");

                if (installCollectionType == null)
                {
                    Console.Error.WriteLine(
                        "Installation collection could not be created.");

                    return 15;
                }

                dynamic? downloadedUpdates =
                    Activator.CreateInstance(
                        installCollectionType);

                if (downloadedUpdates == null)
                {
                    Console.Error.WriteLine(
                        "Installation collection could not be created.");

                    return 16;
                }

                for (int index = 0;
                     index < updatesToInstall.Count;
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

                if (downloadedUpdates.Count == 0)
                {
                    Console.Error.WriteLine(
                        "No updates were downloaded successfully.");

                    return 20;
                }

                Console.WriteLine();

                Console.WriteLine(
                    $"Installing {downloadedUpdates.Count} update(s)...");

                dynamic installer =
                    session.CreateUpdateInstaller();

                installer.ClientApplicationID =
                    "WinBoost Pro 11";

                installer.Updates =
                    downloadedUpdates;

                if (installer.IsBusy)
                {
                    Console.Error.WriteLine(
                        "Windows Update is already installing or removing updates.");

                    return 21;
                }

                if (installer.RebootRequiredBeforeInstallation)
                {
                    Console.Error.WriteLine(
                        "Windows requires a restart before updates can be installed.");

                    return 22;
                }

                object? installationResult =
                    RunInstallationWithProgress(
                        installer,
                        downloadedUpdates);

                if (installationResult == null)
                {
                    return 23;
                }

                dynamic dynamicInstallationResult =
                    installationResult;

                Console.WriteLine();

                Console.WriteLine(
                    $"Installation result code: " +
                    $"{dynamicInstallationResult.ResultCode}");

                Console.WriteLine(
                    $"Restart required: " +
                    $"{dynamicInstallationResult.RebootRequired}");

                for (int index = 0;
                     index < downloadedUpdates.Count;
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

                Console.WriteLine();

                Console.WriteLine(
                    "Windows Update operation completed.");

                return 0;
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

                return 100;
            }
        }

        private static object? RunDownloadWithActivityIndicator(
            dynamic downloader)
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
                    DateTime.Now - startedAt;

                Console.WriteLine(
                    $"Download in progress... elapsed " +
                    $"{elapsed:mm\\:ss}");
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
            dynamic downloadedUpdates)
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
                                $"{downloadedUpdates.Count} | " +
                                $"{currentUpdatePercent}% | " +
                                $"{elapsed:mm\\:ss}");

                            if (!string.IsNullOrWhiteSpace(
                                currentTitle))
                            {
                                Console.WriteLine(
                                    $"Current: {currentTitle}");
                            }

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
                        // Cleanup failure must not hide
                        // the installation result.
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
    }
}