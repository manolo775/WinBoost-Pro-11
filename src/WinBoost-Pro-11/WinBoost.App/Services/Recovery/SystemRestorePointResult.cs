namespace WinBoost.App.Services.Recovery
{
    public sealed class SystemRestorePointResult
    {
        public bool IsSuccessful
        {
            get;
            init;
        }

        public uint ReturnCode
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