namespace WinBoost.Licensing.Server.Models
{
    public sealed class PaymentCustomerResult
    {
        public bool Success
        {
            get;
            init;
        }

        public string CustomerId
        {
            get;
            init;
        } = string.Empty;

        public string Email
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