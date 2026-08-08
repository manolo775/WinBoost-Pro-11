namespace WinBoost.App.Models
{
    public sealed class PerformanceAlertSettings
    {
        public bool AlertsEnabled
        {
            get;
            set;
        } = true;

        public bool EnableSoundForCriticalAlerts
        {
            get;
            set;
        } = false;

        public int CriticalAlertRepeatIntervalMinutes
        {
            get;
            set;
        } = 15;

        public bool CpuAlertsEnabled
        {
            get;
            set;
        } = true;

        public bool RamAlertsEnabled
        {
            get;
            set;
        } = true;

        public bool DiskAlertsEnabled
        {
            get;
            set;
        } = true;

        public bool CpuTemperatureAlertsEnabled
        {
            get;
            set;
        } = true;

        public double CpuWarningThreshold
        {
            get;
            set;
        } = 80.0;

        public double CpuCriticalThreshold
        {
            get;
            set;
        } = 90.0;

        public double RamWarningThreshold
        {
            get;
            set;
        } = 85.0;

        public double RamCriticalThreshold
        {
            get;
            set;
        } = 95.0;

        public double DiskWarningThreshold
        {
            get;
            set;
        } = 85.0;

        public double DiskCriticalThreshold
        {
            get;
            set;
        } = 95.0;

        public double CpuTemperatureWarningThreshold
        {
            get;
            set;
        } = 80.0;

        public double CpuTemperatureCriticalThreshold
        {
            get;
            set;
        } = 90.0;

        public int SustainedDurationSeconds
        {
            get;
            set;
        } = 30;

        public int AlertCooldownMinutes
        {
            get;
            set;
        } = 15;
    }
}