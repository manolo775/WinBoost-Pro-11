namespace WinBoost.Licensing.Server.Configuration
{
    public sealed class UpdateManifestOptions
    {
        public const string SectionName =
            "UpdateManifest";

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