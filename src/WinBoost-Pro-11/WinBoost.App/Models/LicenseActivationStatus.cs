namespace WinBoost.App.Models
{
    public enum LicenseActivationStatus
    {
        Success = 0,
        InvalidKey = 1,
        AlreadyActivated = 2,
        Expired = 3,
        ServerUnavailable = 4,
        NetworkError = 5,
        Error = 6
    }
}