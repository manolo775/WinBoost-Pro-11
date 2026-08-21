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
      "https://localhost:7160/api/licensing/check-activation";

        public static string RevocationCheckEndpoint =>
    "https://localhost:7160/api/licensing/check-revocation";

        public static string PurchasePageUrl =>
            string.Empty;

        public static string PublicKeyPem =>
            """"
    public static string PublicKeyPem =>
    """
    -----BEGIN PUBLIC KEY-----
    MIIBojANBgkqhkiG9w0BAQEFAAOCAY8AMIIBigKCAYEArcXSlpfBrMp9nQSic4ll
    DvWgFZ0tfjzu9gt5YQ0ocgsWbD27zFg90A1wxEKgW6INatY3QRdob/AKIjPCGdH+
    9PorOdy+1YSyMx1rTzmFCe3DSMccfwtTcvxmX1h67nlMMXK9j8Fz/J0Txo2Qfqqg
    6+0dkqZ5HENJfUjpgibSIY119XZfm5GpZT4U/+/e/dXeEq3WDE/e6oGXXBD9t+oV
    wKZFSmg9Ta4j51TDH8SfmAozBJEJ7BPpLfk8Z7/xY0D83KJbploRKunMOfdc/5I+
    L1qR7ZrGMdeowNYHgHyQuKZEhqhuicpKPA2hjkI8MU3FwGo+I5Cj9puDs2mkxHzw
    nd0XQs6Wb81eD4iz5DkJoeRWM9pTDYhnTlUctixkydkaYL6iXR1PviKFuZROIM0F
    SBThBvvc1Evkwaml3C85+Il4j3tINswMXGlhclTUodkuIlClZoCniXkZ+/O4IcKA
    Oz3GLWvir09sKw9MWwaY5BKTVrRGgvRtP0YUiCA55o4VAgMBAAE=
    -----END PUBLIC KEY-----
    """;
    """";

        public static bool HasPublicKey =>
            !string.IsNullOrWhiteSpace(
                PublicKeyPem);
    }
}