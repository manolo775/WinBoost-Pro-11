namespace WinBoost.Licensing.Server.Models
{
    public sealed class TrialActivationRequest
    {
        public string DeviceId
        {
            get;
            set;
        } = string.Empty;

        public string TrialDeviceToken
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