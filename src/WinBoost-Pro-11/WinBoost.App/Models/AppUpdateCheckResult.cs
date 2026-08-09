namespace WinBoost.App.Models
{
    public enum AppUpdateStatus
    {
        NotChecked,
        Checking,
        UpdateAvailable,
        Updating,
        Updated,
        UpToDate,
        Unavailable,
        Failed
    }

    public sealed class AppUpdateCheckResult
    {
        public AppUpdateStatus Status { get; set; } =
            AppUpdateStatus.NotChecked;

        public string PackageId { get; set; } =
            string.Empty;

        public string AvailableVersion { get; set; } =
            string.Empty;

        public string Details { get; set; } =
            string.Empty;
    }
}