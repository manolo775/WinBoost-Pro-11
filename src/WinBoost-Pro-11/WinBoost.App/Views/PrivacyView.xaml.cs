using System.Windows.Controls;
using WinBoost.App.ViewModels;

namespace WinBoost.App.Views
{
    public partial class PrivacyView : UserControl
    {
        public PrivacyView()
        {
            InitializeComponent();

            DataContext = new PrivacyViewModel();
        }
    }
}