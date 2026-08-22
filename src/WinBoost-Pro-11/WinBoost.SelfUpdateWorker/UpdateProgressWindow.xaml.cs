using System;
using System.Windows;

namespace WinBoost.SelfUpdateWorker
{
    public partial class UpdateProgressWindow : Window
    {
        public UpdateProgressWindow()
        {
            InitializeComponent();
        }

        // ======================================
        // UPDATE STATUS + PROGRESS
        // ======================================

        public void UpdateProgress(
            string statusText,
            double progressValue)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(
                    () =>
                        UpdateProgress(
                            statusText,
                            progressValue));

                return;
            }

            StatusTextBlock.Text =
                statusText;

            UpdateProgressBar.Value =
                Math.Clamp(
                    progressValue,
                    0,
                    100);
        }

        // ======================================
        // UPDATE STATUS ONLY
        // ======================================

        public void UpdateStatus(
            string statusText)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(
                    () =>
                        UpdateStatus(
                            statusText));

                return;
            }

            StatusTextBlock.Text =
                statusText;
        }

        // ======================================
        // UPDATE PROGRESS ONLY
        // ======================================

        public void UpdateProgressValue(
            double progressValue)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(
                    () =>
                        UpdateProgressValue(
                            progressValue));

                return;
            }

            UpdateProgressBar.Value =
                Math.Clamp(
                    progressValue,
                    0,
                    100);
        }
    }
}