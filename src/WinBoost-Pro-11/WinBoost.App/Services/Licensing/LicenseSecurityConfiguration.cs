namespace WinBoost.App.Services.Licensing
{
    public static class LicenseSecurityConfiguration
    {
        public const string ProductName =
            "WinBoost Pro 11";

        public static string ActivationEndpoint =>
            string.Empty;

        public static string PurchaseSessionEndpoint =>
     "https://localhost:7160/api/licensing/purchase-session";

        public static string LicenseOffersEndpoint =>
     "https://localhost:7160/api/licensing/offers";
        public static string ActivationCheckEndpoint =>
            string.Empty;

        public static string PurchasePageUrl =>
            string.Empty;

        public static string PublicKeyPem =>
            string.Empty;

        public static bool HasPublicKey =>
            !string.IsNullOrWhiteSpace(
                PublicKeyPem);
    }
}