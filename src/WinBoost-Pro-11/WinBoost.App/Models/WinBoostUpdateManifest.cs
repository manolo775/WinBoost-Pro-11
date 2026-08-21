namespace WinBoost.App.Models
{
    public sealed class WinBoostUpdateManifest
    {
        public string Version
        {
            get;
            set;
        } = string.Empty;

        public string Channel
        {
            get;
            set;
        } = string.Empty;

        public string DownloadUrl
        {
            get;
            set;
        } = string.Empty;

        public string Sha256
        {
            get;
            set;
        } = string.Empty;

        public string ReleaseNotes
        {
            get;
            set;
        } = string.Empty;
    }
}