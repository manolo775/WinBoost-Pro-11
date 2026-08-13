using System;
using System.Diagnostics;
using System.Windows;
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

        private void OpenWindowsUpdate_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                Process.Start(
                    new ProcessStartInfo
                    {
                        FileName = "ms-settings:windowsupdate",
                        UseShellExecute = true
                    });
            }
            catch (Exception)
            {
                MessageBox.Show(
                    "Windows Update could not be opened.",
                    "WinBoost Pro 11",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
    }
}