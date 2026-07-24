using System.Windows;
using WinBoost.App.Views;

namespace WinBoost.App
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            MainContent.Content = new DashboardView();
        }

        private void DashboardButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            MainContent.Content = new DashboardView();
        }

        private void PerformanceButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            MainContent.Content = new PerformanceView();
        }
    }
}