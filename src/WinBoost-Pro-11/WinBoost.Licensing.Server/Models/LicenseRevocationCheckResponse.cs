namespace WinBoost.Licensing.Server.Models
{
    public sealed class LicenseRevocationCheckResponse
    {
        public bool Success
        {
            get;
            set;
        }

        public bool IsRevoked
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
    }
}