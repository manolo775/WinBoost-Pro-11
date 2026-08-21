namespace WinBoost.Licensing.Server.Models
{
    public sealed class TrialActivationResponse
    {
        public bool Success
        {
            get;
            set;
        }

        public string ErrorCode
        {
            get;
            set;
        } = string.Empty;

        public string Message
        {
            get;
            set;
        } = string.Empty;

        public SignedLicenseResponse? License
        {
            get;
            set;
        }
    }
}