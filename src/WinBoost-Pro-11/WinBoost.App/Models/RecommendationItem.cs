using System.Windows.Media;
using MaterialDesignThemes.Wpf;

namespace WinBoost.App.Models
{
    public sealed class RecommendationItem
    {
        public PackIconKind Icon { get; set; }

        public Brush IconBrush { get; set; } =
            Brushes.White;

        public string Text { get; set; } =
            string.Empty;
    }
}