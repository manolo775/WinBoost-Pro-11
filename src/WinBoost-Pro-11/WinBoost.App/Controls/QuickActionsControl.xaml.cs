using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using WinBoost.App.Models;
using WinBoost.App.Services;

namespace WinBoost.App.Controls
{
    public partial class QuickActionsControl : UserControl
    {
        private readonly TempFilesCleanerService
            _tempFilesCleanerService;

        private bool _isCleaningTempFiles;

        public QuickActionsControl()
        {
            InitializeComponent();

            _tempFilesCleanerService =
                new TempFilesCleanerService();
        }

        private void OpenStartupButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            Window? window =
                Window.GetWindow(this);

            if (window is MainWindow mainWindow)
            {
                mainWindow.NavigateToStartup();
            }
        }

        private async void CleanTempFilesButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_isCleaningTempFiles)
            {
                return;
            }

            _isCleaningTempFiles = true;

            CleanTempFilesButton.IsEnabled = false;
            CleanTempFilesButton.Content = "Se curăță...";

            try
            {
                OptimizationResult result =
                    await _tempFilesCleanerService
                        .CleanUserTempAsync();

                string message =
                    result.IsSuccessful
                        ? $"{result.Message}\n\n" +
                          $"Spațiu eliberat: " +
                          $"{result.RecoveredSpaceText}"
                        : result.Message;

                MessageBox.Show(
                    message,
                    result.IsSuccessful
                        ? "Curățare finalizată"
                        : "Eroare la curățare",
                    MessageBoxButton.OK,
                    result.IsSuccessful
                        ? MessageBoxImage.Information
                        : MessageBoxImage.Warning);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(
                    $"Curățarea nu a putut fi finalizată:\n\n" +
                    $"{ex.Message}",
                    "Eroare",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                CleanTempFilesButton.Content =
                    "Clean Temp Files";

                CleanTempFilesButton.IsEnabled = true;

                _isCleaningTempFiles = false;
            }
        }
    }
}