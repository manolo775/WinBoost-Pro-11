namespace WinBoost.Licensing.Server.Configuration
{
    public sealed class LicenseSigningOptions
    {
        public const string SectionName =
            "LicenseSigning";

        public string PrivateKeyPem
        {
            get;
            set;
        } = string.Empty;
    }
}