namespace WinBoost.App.Models
{
    public sealed class PerformanceHistorySettings
    {
        public int RetentionDays
        {
            get;
            set;
        } = 14;
    }
}