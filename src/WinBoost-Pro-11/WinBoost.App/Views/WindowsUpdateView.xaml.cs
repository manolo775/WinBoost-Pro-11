using System.Windows.Controls;
using WinBoost.App.ViewModels;

namespace WinBoost.App.Views
{
    public partial class WindowsUpdateView : UserControl
    {
        private readonly WindowsUpdateViewModel _viewModel;

        public WindowsUpdateView()
        {
            InitializeComponent();

            _viewModel = new WindowsUpdateViewModel();
            DataContext = _viewModel;
        }
    }
}