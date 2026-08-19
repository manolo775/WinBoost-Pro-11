namespace WinBoost.App.Models
{
    public sealed class PurchaseSessionRequest
    {
        public string CustomerEmail
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

        public LicensePlan Plan
        {
            get;
            init;
        } = LicensePlan.Unknown;

    }
}