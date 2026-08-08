namespace WinBoost.App.Models
{
    public enum PerformanceAnalysisRecommendationType
    {
        InsufficientData,
        Good,
        CpuHigh,
        CpuIncreasing,
        CpuTemperatureHigh,
        RamHigh,
        RamIncreasing,
        DiskHigh,
        DiskIncreasing,
        MultipleIssues
    }

    public enum PerformanceAnalysisSeverity
    {
        Information,
        Good,
        Warning,
        Critical
    }

    public sealed class
        PerformanceAnalysisRecommendation
    {
        public PerformanceAnalysisRecommendationType
            Type
        {
            get;
            set;
        }

        public PerformanceAnalysisSeverity
            Severity
        {
            get;
            set;
        }
    }
}