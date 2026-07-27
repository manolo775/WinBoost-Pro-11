using System.Windows;
using System.Windows.Controls;
using WinBoost.App.ViewModels;

namespace WinBoost.App.Views
{
    public partial class PerformanceView : UserControl
    {
        private readonly PerformanceViewModel _viewModel;

        public PerformanceView()
        {
            InitializeComponent();

            _viewModel = new PerformanceViewModel();
            DataContext = _viewModel;

            Loaded += PerformanceView_Loaded;
            Unloaded += PerformanceView_Unloaded;
        }

        private void PerformanceView_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            _viewModel.StartPerformanceMonitoring();
        }

        private void PerformanceView_Unloaded(
            object sender,
            RoutedEventArgs e)
        {
            _viewModel.StopPerformanceMonitoring();
        }
    }
}