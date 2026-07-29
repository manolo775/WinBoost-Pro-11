using System.Windows.Controls;
using WinBoost.App.ViewModels;

namespace WinBoost.App.Views
{
    public partial class StartupView : UserControl
    {
        public StartupView()
        {
            InitializeComponent();

            DataContext = new StartupViewModel();
        }
    }
}