namespace WinBoost.App.Models
{
    public class StartupAppInfo
    {
        public string Name { get; set; } =
            string.Empty;

        public string Command { get; set; } =
            string.Empty;

        public string Source { get; set; } =
            string.Empty;

        public bool IsEnabled { get; set; }

        public string Status =>
            IsEnabled
                ? "Activat"
                : "Dezactivat";
    }
}