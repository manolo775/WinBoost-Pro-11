namespace WinBoost.App.Models
{
    public class SystemHealthRawData
    {
        public ServicesHealthData Services
        {
            get;
        } =
            new ServicesHealthData();

        public StartupHealthData Startup
        {
            get;
        } =
            new StartupHealthData();

        public PrivacyHealthData Privacy
        {
            get;
        } =
            new PrivacyHealthData();

        public WindowsUpdateHealthData WindowsUpdate
        {
            get;
        } =
            new WindowsUpdateHealthData();
    }

    public class ServicesHealthData
    {
        public int TotalServices { get; set; }

        public int CriticalServices { get; set; }

        public int OptionalServices { get; set; }

        public int LowRiskServices { get; set; }
    }

    public class StartupHealthData
    {
        public int TotalStartupApps { get; set; }

        public int EnabledStartupApps { get; set; }
    }

    public class PrivacyHealthData
    {
        public int TotalChecks { get; set; }

        public int PassedChecks { get; set; }
    }

    public class WindowsUpdateHealthData
    {
        public int PendingUpdates { get; set; }

        public bool RequiresRestart { get; set; }
    }
}