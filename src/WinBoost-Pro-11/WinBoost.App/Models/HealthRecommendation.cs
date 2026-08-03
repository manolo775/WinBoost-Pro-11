namespace WinBoost.App.Models
{
    public class HealthRecommendation
    {
        public string Title
        {
            get;
            set;
        } = string.Empty;

        public string Description
        {
            get;
            set;
        } = string.Empty;

        public string Impact
        {
            get;
            set;
        } = string.Empty;

        public string Recommendation
        {
            get;
            set;
        } = string.Empty;

        public int PotentialGain
        {
            get;
            set;
        }

        public bool CanAutoFix
        {
            get;
            set;
        }

        public string ActionText =>
            CanAutoFix
                ? "Optimize"
                : "Learn More";
    }
}