namespace WinBoost.App.Models
{
    public sealed class LicenseActivationCheckRequest
    {
        public string CustomerEmail
        {
            get;
            init;
        } = string.Empty;

        public string PurchaseSessionId
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
        } = string.Empty;
    }
}