using System.Windows.Controls;
using WinBoost.App.ViewModels;

namespace WinBoost.App.Views
{
    public partial class ServicesView : UserControl
    {
        public ServicesView()
        {
            InitializeComponent();

            DataContext = new ServicesViewModel();
        }
    }
}