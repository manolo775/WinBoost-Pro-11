namespace WinBoost.Licensing.Server.Models
{
    public sealed class LicenseActivationCheckResponse
    {
        public bool Success
        {
            get;
            init;
        }

        public bool PaymentCompleted
        {
            get;
            init;
        }

        public string ErrorCode
        {
            get;
            init;
        } = string.Empty;

        public string Message
        {
            get;
            init;
        } = string.Empty;

        public SignedLicenseResponse? License
        {
            get;
            init;
        }
    }
}