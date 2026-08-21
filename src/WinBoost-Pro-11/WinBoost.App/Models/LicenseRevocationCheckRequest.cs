namespace WinBoost.App.Models
{
    public sealed class LicenseRevocationCheckRequest
    {
        public string LicenseId
        {
            get;
            set;
        } = string.Empty;

        public string DeviceId
        {
            get;
            set;
        } = string.Empty;

        public string ProductName
        {
            get;
            set;
        } = string.Empty;
    }
}