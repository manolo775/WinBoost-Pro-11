using System.Windows;
using System.Windows.Controls;
using WinBoost.App.ViewModels;

namespace WinBoost.App.Views
{
    public partial class RecoveryView : UserControl
    {
        private readonly RecoveryViewModel
            _viewModel;

        public RecoveryView()
        {
            InitializeComponent();

            _viewModel =
                new RecoveryViewModel();

            DataContext =
                _viewModel;

            Loaded +=
                RecoveryView_Loaded;
        }

        private async void RecoveryView_Loaded(
    object sender,
    RoutedEventArgs e)
        {
            Loaded -=
                RecoveryView_Loaded;

            await _viewModel
                .CheckAvailabilityAsync();

            await _viewModel
                .LoadCachedRestorePointsAsync();

            await _viewModel
                .CheckPendingRestoreResultAsync();
        }
    }
}