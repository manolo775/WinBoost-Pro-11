namespace WinBoost.Licensing.Server.Models
{
    public sealed class PaymentCheckoutResult
    {
        public bool Success
        {
            get;
            init;
        }

        public string CheckoutUrl
        {
            get;
            init;
        } = string.Empty;

        public string ProviderSessionId
        {
            get;
            init;
        } = string.Empty;

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