using System;
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

        private readonly OptimizationEngine
            _optimizationEngine;

        private bool _isCleaningTempFiles;
        private bool _isOptimizingSystem;

        public QuickActionsControl()
        {
            InitializeComponent();

            _tempFilesCleanerService =
                new TempFilesCleanerService();

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

            MessageBoxResult confirmation =
                MessageBox.Show(
                    "WinBoost va executa optimizările disponibile.\n\n" +
                    "În această etapă va curăța fișierele temporare " +
                    "ale utilizatorului.\n\n" +
                    "Dorești să continui?",
                    "Confirmare optimizare",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

            if (confirmation != MessageBoxResult.Yes)
            {
                return;
            }

            _isOptimizingSystem = true;

            OptimizeSystemButton.IsEnabled = false;
            CleanTempFilesButton.IsEnabled = false;

            OptimizeSystemButton.Content =
                "Se optimizează...";

            try
            {
                OptimizationReport report =
                    await _optimizationEngine
                        .RunOptimizationAsync();

                string message =
                    $"{report.Message}\n\n" +
                    $"Fișiere șterse: " +
                    $"{report.TotalDeletedFiles}\n" +
                    $"Spațiu eliberat: " +
                    $"{report.RecoveredSpaceText}\n" +
                    $"Durată: {report.DurationText}";

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
    }
}