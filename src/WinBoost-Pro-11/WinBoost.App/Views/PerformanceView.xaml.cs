using System.Windows.Controls;
using WinBoost.App.ViewModels;

namespace WinBoost.App.Views
{
    public partial class PerformanceView : UserControl
    {
        public PerformanceView()
        {
            InitializeComponent();

            DataContext = new PerformanceViewModel();
        }
    }
}