using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WinBoost.App.Services.WindowsUpdate
{
    public sealed class WindowsUpdateAvailableInfo
    {
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

        public bool IsDownloaded
        {
            get;
            init;
        }

        public bool RebootRequired
        {
            get;
            init;
        }
    }

    public sealed class WindowsUpdateAvailableResult
    {
        public IReadOnlyList<WindowsUpdateAvailableInfo>
            Updates
        {
            get;
            init;
        } =
            Array.Empty<WindowsUpdateAvailableInfo>();

        public int UpdateCount =>
            Updates.Count;
    }

    public sealed class WindowsUpdateAvailableScanner
    {
        public Task<WindowsUpdateAvailableResult>
            ScanAsync()
        {
            return Task.Run(
                ScanInternal);
        }

        private static WindowsUpdateAvailableResult
            ScanInternal()
        {
            Type? sessionType =
                Type.GetTypeFromProgID(
                    "Microsoft.Update.Session");

            if (sessionType == null)
            {
                throw new InvalidOperationException(
                    "Windows Update Agent is not available.");
            }

            dynamic? session =
                Activator.CreateInstance(
                    sessionType);

            if (session == null)
            {
                throw new InvalidOperationException(
                    "Windows Update session could not be created.");
            }

            session.ClientApplicationID =
                "WinBoost Pro 11";

            dynamic searcher =
                session.CreateUpdateSearcher();

            dynamic searchResult =
                searcher.Search(
                    "IsInstalled=0 and IsHidden=0");

            var updates =
                new List<WindowsUpdateAvailableInfo>();

            for (int index = 0;
                 index < searchResult.Updates.Count;
                 index++)
            {
                dynamic update =
                    searchResult.Updates.Item(index);

                updates.Add(
                    new WindowsUpdateAvailableInfo
                    {
                        Title =
                            Convert.ToString(
                                update.Title)
                            ?? string.Empty,

                        Description =
                            Convert.ToString(
                                update.Description)
                            ?? string.Empty,

                        UpdateId =
                            Convert.ToString(
                                update.Identity.UpdateID)
                            ?? string.Empty,

                        IsDownloaded =
                            Convert.ToBoolean(
                                update.IsDownloaded),

                        RebootRequired =
                            Convert.ToBoolean(
                                update.RebootRequired)
                    });
            }

            return new WindowsUpdateAvailableResult
            {
                Updates =
                    updates
            };
        }
    }
}