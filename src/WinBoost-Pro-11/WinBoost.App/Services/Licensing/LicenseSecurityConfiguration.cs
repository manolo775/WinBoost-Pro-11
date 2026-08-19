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

        public static string PurchasePageUrl =>
    string.Empty;

        /*
         * IMPORTANT:
         *
         * Only the PUBLIC licensing key
         * will be stored in the application.
         *
         * The PRIVATE key must exist only
         * on the WinBoost licensing server.
         *
         * This value remains empty until
         * the server-side licensing system
         * is created.
         */
        public static string PublicKeyPem =>
            string.Empty;

        public static bool HasPublicKey =>
            !string.IsNullOrWhiteSpace(
                PublicKeyPem);
    }
}