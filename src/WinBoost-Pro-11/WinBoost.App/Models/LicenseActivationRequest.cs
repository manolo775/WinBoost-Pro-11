namespace WinBoost.App.Models
{
    public sealed class LicenseActivationRequest
    {
        public string CustomerEmail
        {
            get;
            init;
        } = string.Empty;

        public string ActivationToken
        {
            get;
            init;
        } = string.Empty;

        public string DeviceId
        {
            get;
            init;
        } = string.Empty;

        public string ProductName
        {
            get;
            init;
        } = "WinBoost Pro 11";
    }
}