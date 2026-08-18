namespace WinBoost.App.Models
{
    public sealed class LicenseActivationApiResponse
    {
        public bool Success
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