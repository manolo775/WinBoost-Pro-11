namespace WinBoost.App.Models
{
    public sealed class LicenseActivationResult
    {
        public LicenseActivationStatus Status
        {
            get;
            init;
        }

        public LicenseInfo? License
        {
            get;
            init;
        }

        public string Message
        {
            get;
            init;
        } = string.Empty;

        public bool IsSuccessful =>
            Status ==
            LicenseActivationStatus.Success &&
            License != null;
    }
}