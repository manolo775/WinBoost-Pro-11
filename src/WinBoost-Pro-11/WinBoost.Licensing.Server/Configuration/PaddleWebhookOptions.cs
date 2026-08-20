namespace WinBoost.Licensing.Server.Configuration
{
    public sealed class PaddleWebhookOptions
    {
        public const string SectionName =
            "PaddleWebhook";

        public string SecretKey
        {
            get;
            set;
        } = string.Empty;
    }
}