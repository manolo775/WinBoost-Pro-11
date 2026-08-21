namespace WinBoost.App.Models
{
    public enum WinBoostUpdateStatus
    {
        NotChecked,
        Checking,
        UpToDate,
        UpdateAvailable,
        Unavailable,
        Failed
    }

    public sealed class WinBoostUpdateCheckResult
    {
        public WinBoostUpdateStatus Status
        {
            get;
            set;
        } = WinBoostUpdateStatus.NotChecked;

        public string CurrentVersion
        {
            get;
            set;
        } = string.Empty;

        public string AvailableVersion
        {
            get;
            set;
        } = string.Empty;

        public string Channel
        {
            get;
            set;
        } = string.Empty;

        public string DownloadUrl
        {
            get;
            set;
        } = string.Empty;

        public string Sha256
        {
            get;
            set;
        } = string.Empty;

        public string ReleaseNotes
        {
            get;
            set;
        } = string.Empty;

        public string Details
        {
            get;
            set;
        } = string.Empty;
    }
}