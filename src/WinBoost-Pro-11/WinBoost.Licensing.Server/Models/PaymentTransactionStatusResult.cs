namespace WinBoost.Licensing.Server.Models
{
    public sealed class PaymentTransactionStatusResult
    {
        public bool Success
        {
            get;
            init;
        }

        public string ProviderSessionId
        {
            get;
            init;
        } = string.Empty;

        public string Status
        {
            get;
            init;
        } = string.Empty;

        public bool PaymentCompleted
        {
            get;
            init;
        }

        public string PriceId
        {
            get;
            init;
        } = string.Empty;

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
    }
}