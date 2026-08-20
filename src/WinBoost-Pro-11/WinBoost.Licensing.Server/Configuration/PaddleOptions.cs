namespace WinBoost.Licensing.Server.Configuration
{
    public sealed class PaddleOptions
    {
        public const string SectionName =
            "Paddle";

        public string ApiKey
        {
            get;
            set;
        } = string.Empty;

        public string ClientSideToken
        {
            get;
            set;
        } = string.Empty;

        public string BaseUrl
        {
            get;
            set;
        } =
            "https://sandbox-api.paddle.com";

        public string PromotionalLifetimePriceId
        {
            get;
            set;
        } = string.Empty;

        public string OneMonthPriceId
        {
            get;
            set;
        } = string.Empty;

        public string ThreeMonthsPriceId
        {
            get;
            set;
        } = string.Empty;

        public string SixMonthsPriceId
        {
            get;
            set;
        } = string.Empty;

        public string OneYearPriceId
        {
            get;
            set;
        } = string.Empty;
    }
}