namespace WinBoost.App.Models
{
    public sealed class WinBoostUpdateDownloadResult
    {
        public bool Success
        {
            get;
            set;
        }

        public string FilePath
        {
            get;
            set;
        } = string.Empty;

        public string ExpectedSha256
        {
            get;
            set;
        } = string.Empty;

        public string ActualSha256
        {
            get;
            set;
        } = string.Empty;

        public string ErrorCode
        {
            get;
            set;
        } = string.Empty;

        public string Details
        {
            get;
            set;
        } = string.Empty;
    }
}