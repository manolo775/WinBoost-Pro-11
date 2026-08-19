namespace WinBoost.Licensing.Server.Models
{
    public sealed class PurchaseSessionResponse
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

        public string SessionId
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