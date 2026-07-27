using System.Windows.Media;

namespace WinBoost.App.Models
{
    public class WindowsServiceInfo
    {
        public string DisplayName { get; set; } =
            string.Empty;

        public string ServiceName { get; set; } =
            string.Empty;

        public string Status { get; set; } =
            string.Empty;

        public string StartType { get; set; } =
            string.Empty;

        public string Recommendation { get; set; } =
            string.Empty;

        public Brush StatusBrush { get; set; } =
            Brushes.LightGray;
    }
}