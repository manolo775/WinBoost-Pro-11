namespace WinBoost.App.Services.Licensing
{
    public static class LicenseSecurityConfiguration
    {
        public const string ProductName =
            "WinBoost Pro 11";

        public static string ActivationEndpoint =>
            string.Empty;

        public static string PurchaseSessionEndpoint =>
            string.Empty;

        public static string LicenseOffersEndpoint =>
            string.Empty;

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