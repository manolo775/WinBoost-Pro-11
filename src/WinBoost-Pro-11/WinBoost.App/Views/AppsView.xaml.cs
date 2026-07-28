using System.Windows.Controls;
using WinBoost.App.ViewModels;

namespace WinBoost.App.Views
{
    public partial class AppsView : UserControl
    {
        public AppsView()
        {
            InitializeComponent();

            DataContext = new AppsViewModel();
        }
    }
}