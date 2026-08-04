namespace WinBoost.App.Services.Health
{
    public sealed class WinBoostHealthScoreService
    {
        private static readonly WinBoostHealthScoreService
            _instance =
                new();

        public static WinBoostHealthScoreService
            Instance =>
                _instance;

        private WinBoostHealthScoreService()
        {
        }

        public int PerformanceScore
        {
            get;
            set;
        } = 100;

        public int PrivacyScore
        {
            get;
            set;
        } = 100;

        public int ServicesScore
        {
            get;
            set;
        } = 100;

        public int StartupScore
        {
            get;
            set;
        } = 100;

        public int OverallScore =>
            (PerformanceScore +
             PrivacyScore +
             ServicesScore +
             StartupScore) / 4;
    }
}