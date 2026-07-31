using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using WinBoost.App.Models;
using WinBoost.App.Services.Optimization;

namespace WinBoost.App.Controls
{
    public partial class QuickActionsControl : UserControl
    {
        private readonly TempFilesCleanerService
            _tempFilesCleanerService;

        private readonly RecycleBinCleanerService
            _recycleBinCleanerService;

        private readonly OptimizationEngine
            _optimizationEngine;

        private bool _isCleaningTempFiles;
        private bool _isOptimizingSystem;

        public QuickActionsControl()
        {
            InitializeComponent();

            _tempFilesCleanerService =
                new TempFilesCleanerService();

            _recycleBinCleanerService =
                new RecycleBinCleanerService();

            _optimizationEngine =
                new OptimizationEngine();
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
            if (_isCleaningTempFiles ||
                _isOptimizingSystem)
            {
                return;
            }

            _isCleaningTempFiles = true;

            CleanTempFilesButton.IsEnabled = false;
            OptimizeSystemButton.IsEnabled = false;

            CleanTempFilesButton.Content =
                "Se curăță...";

            try
            {
                OptimizationResult result =
                    await _tempFilesCleanerService
                        .CleanUserTempAsync();

                string message =
                    result.IsSuccessful
                        ? $"{result.Message}\n\n" +
                          $"Fișiere șterse: " +
                          $"{result.DeletedFilesCount}\n" +
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
            catch (Exception ex)
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
                OptimizeSystemButton.IsEnabled = true;

                _isCleaningTempFiles = false;
            }
        }

        private async void OptimizeSystemButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_isOptimizingSystem ||
                _isCleaningTempFiles)
            {
                return;
            }

            MessageBoxResult startConfirmation =
                MessageBox.Show(
                    "WinBoost va executa optimizările disponibile.\n\n" +
                    "În această etapă va curăța fișierele temporare " +
                    "ale utilizatorului.\n\n" +
                    "Dorești să continui?",
                    "Confirmare optimizare",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

            if (startConfirmation != MessageBoxResult.Yes)
            {
                return;
            }

            RecycleBinStatus recycleBinStatus =
                await _recycleBinCleanerService
                    .GetRecycleBinStatusAsync();

            string recycleBinMessage;

            if (recycleBinStatus.IsSuccessful)
            {
                recycleBinMessage =
                    $"Coșul de reciclare conține:\n\n" +
                    $"• Elemente: {recycleBinStatus.ItemCount}\n" +
                    $"• Spațiu ocupat: " +
                    $"{recycleBinStatus.TotalSizeText}\n\n" +
                    $"Dorești să golești și Coșul de reciclare?\n\n" +
                    $"Fișierele vor fi șterse definitiv și nu " +
                    $"vor mai putea fi restaurate.";
            }
            else
            {
                recycleBinMessage =
                    "Nu s-au putut citi informațiile despre " +
                    "Coșul de reciclare.\n\n" +
                    "Dorești totuși să încerci golirea acestuia?";
            }

            MessageBoxResult recycleBinConfirmation =
                MessageBox.Show(
                    recycleBinMessage,
                    "Golire Coș de reciclare",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

            bool emptyRecycleBin =
                recycleBinConfirmation == MessageBoxResult.Yes;

            _isOptimizingSystem = true;

            OptimizeSystemButton.IsEnabled = false;
            CleanTempFilesButton.IsEnabled = false;

            OptimizeSystemButton.Content =
                "Se optimizează...";

            try
            {
                OptimizationReport report =
                    await _optimizationEngine
                        .RunOptimizationAsync(
                            emptyRecycleBin);

                string message =
                    BuildOptimizationReportMessage(
                        report,
                        emptyRecycleBin);

                MessageBox.Show(
                    message,
                    report.IsSuccessful
                        ? "Optimizare finalizată"
                        : "Optimizare finalizată cu erori",
                    MessageBoxButton.OK,
                    report.IsSuccessful
                        ? MessageBoxImage.Information
                        : MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Optimizarea nu a putut fi finalizată:\n\n" +
                    $"{ex.Message}",
                    "Eroare",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                OptimizeSystemButton.Content =
                    "Optimize System";

                OptimizeSystemButton.IsEnabled = true;
                CleanTempFilesButton.IsEnabled = true;

                _isOptimizingSystem = false;
            }
        }

        private static string BuildOptimizationReportMessage(
            OptimizationReport report,
            bool recycleBinRequested)
        {
            var messageBuilder =
                new StringBuilder();

            messageBuilder.AppendLine(report.Message);
            messageBuilder.AppendLine();

            foreach (OptimizationResult result
                     in report.Results)
            {
                messageBuilder.AppendLine(
                    result.IsSuccessful
                        ? $"✓ {result.Message}"
                        : $"⚠ {result.Message}");
            }

            if (!recycleBinRequested)
            {
                messageBuilder.AppendLine(
                    "• Coșul de reciclare nu a fost golit.");
            }

            messageBuilder.AppendLine();

            messageBuilder.AppendLine(
                $"Elemente eliminate: " +
                $"{report.TotalDeletedFiles}");

            messageBuilder.AppendLine(
                $"Spațiu eliberat: " +
                $"{report.RecoveredSpaceText}");

            messageBuilder.AppendLine(
                $"Durată: {report.DurationText}");

            return messageBuilder.ToString();
        }
    }
}