using System.Windows;
using WinBoost.App.ViewModels;

namespace WinBoost.App
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            DataContext = new MainViewModel();
        }
    }
}