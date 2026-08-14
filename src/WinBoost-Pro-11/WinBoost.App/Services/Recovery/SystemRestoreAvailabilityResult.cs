namespace WinBoost.App.Services.Recovery
{
    public sealed class SystemRestoreAvailabilityResult
    {
        public bool IsAvailable
        {
            get;
            init;
        }

        public string Message
        {
            get;
            init;
        } = string.Empty;
    }
}