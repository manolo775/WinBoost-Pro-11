using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using WinBoost.App.Helpers;
using WinBoost.App.Localization;
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

        private static string T(
            string key,
            params object[] arguments)
        {
            return LocalizationHelper.Format(
                key,
                arguments);
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

        private void OpenServicesButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            Window? window =
                Window.GetWindow(this);

            if (window is MainWindow mainWindow)
            {
                mainWindow.NavigateToServices();
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

            CleanTempFilesText.Text =
                T("QuickActionsTempCleaning");

            try
            {
                OptimizationResult result =
                    await _tempFilesCleanerService
                        .CleanUserTempAsync();

                string message =
                    result.IsSuccessful
                        ? $"{result.Message}\n\n" +
                          $"{T(
                              "QuickActionsTempDeletedFiles",
                              result.DeletedFilesCount)}\n" +
                          $"{T(
                              "QuickActionsTempRecoveredSpace",
                              result.RecoveredSpaceText)}"
                        : result.Message;

                NativeConfirmationDialog.ShowAcknowledgement(
                    Window.GetWindow(this),
                    result.IsSuccessful
                        ? T("QuickActionsTempCleanSuccessTitle")
                        : T("QuickActionsTempCleanErrorTitle"),
                    message,
                    T("CommonYes"),
                    result.IsSuccessful
                        ? MessageBoxImage.Information
                        : MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                NativeConfirmationDialog.ShowAcknowledgement(
                    Window.GetWindow(this),
                    T("CommonError"),
                    $"{T("QuickActionsTempCleanFailed")}\n\n{ex.Message}",
                    T("CommonYes"),
                    MessageBoxImage.Error);
            }
            finally
            {
                CleanTempFilesText.SetResourceReference(
                    TextBlock.TextProperty,
                    "QuickActionsTempFiles");

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

            Window? window =
                Window.GetWindow(this);

            bool confirmed =
                NativeConfirmationDialog.Ask(
                    window,
                    T("QuickActionsOptimizationConfirmationTitle"),
                    T("QuickActionsOptimizationConfirmationMessage"),
                    T("CommonYes"),
                    T("CommonNo"));

            if (!confirmed)
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
                    T(
                        "QuickActionsRecycleBinMessage",
                        recycleBinStatus.ItemCount,
                        recycleBinStatus.TotalSizeText);
            }
            else
            {
                recycleBinMessage =
                    T("QuickActionsRecycleBinReadFailed");
            }

            bool emptyRecycleBin =
                NativeConfirmationDialog.Ask(
                    window,
                    T("QuickActionsRecycleBinTitle"),
                    recycleBinMessage,
                    T("CommonYes"),
                    T("CommonNo"));

            _isOptimizingSystem = true;

            OptimizeSystemButton.IsEnabled = false;
            CleanTempFilesButton.IsEnabled = false;

            OptimizeSystemText.Text =
                T("QuickActionsOptimizing");

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

                NativeConfirmationDialog.ShowAcknowledgement(
                    window,
                    report.IsSuccessful
                        ? T("QuickActionsOptimizationSuccessTitle")
                        : T("QuickActionsOptimizationWarningTitle"),
                    message,
                    T("CommonYes"),
                    report.IsSuccessful
                        ? MessageBoxImage.Information
                        : MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                NativeConfirmationDialog.ShowAcknowledgement(
                    window,
                    T("CommonError"),
                    $"{T("QuickActionsOptimizationFailed")}\n\n{ex.Message}",
                    T("CommonYes"),
                    MessageBoxImage.Error);
            }
            finally
            {
                OptimizeSystemText.SetResourceReference(
                    TextBlock.TextProperty,
                    "QuickActionsOptimize");

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

            messageBuilder.AppendLine(
                report.Message);

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
                    T(
                        "QuickActionsRecycleBinNotEmptied"));
            }

            messageBuilder.AppendLine();

            messageBuilder.AppendLine(
                T(
                    "QuickActionsOptimizationDeletedItems",
                    report.TotalDeletedFiles));

            messageBuilder.AppendLine(
                T(
                    "QuickActionsOptimizationRecoveredSpace",
                    report.RecoveredSpaceText));

            messageBuilder.AppendLine(
                T(
                    "QuickActionsOptimizationDuration",
                    report.DurationText));

            return messageBuilder.ToString();
        }
    }
}