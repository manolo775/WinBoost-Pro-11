using System;
using System.Threading.Tasks;
using System.Windows;
using WinBoost.App.Helpers;
using WinBoost.App.Localization;
using WinBoost.App.Models;
using WinBoost.App.Services.ServicesManager;

namespace WinBoost.App.ViewModels
{
    public class ServiceStartupTypeViewModel
    {
        private readonly WindowsServiceStartupManager
            _startupManager;

        public ServiceStartupTypeViewModel()
        {
            _startupManager =
                new WindowsServiceStartupManager();
        }

        public async Task<bool> ApplyStartupTypeAsync(
            WindowsServiceInfo service)
        {
            if (service == null)
            {
                return false;
            }

            if (!service.CanChangeStartupType)
            {
                MessageBox.Show(
                    LocalizationHelper.Get(
                        "ServicesStartupTypeBlockedMessage"),
                    LocalizationHelper.Get(
                        "ServicesStartupTypeBlockedTitle"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                service.CancelStartupTypeChange();

                return false;
            }

            if (!service.HasStartupTypeChanged)
            {
                return true;
            }

            string previousStartupType =
                service.StartType;

            string selectedStartupType =
                service.SelectedStartupType;

            bool confirmed =
                NativeConfirmationDialog.Ask(
                    Application.Current.MainWindow,
                    LocalizationHelper.Get(
                        "ServicesStartupTypeConfirmationTitle"),
                    LocalizationHelper.Format(
                        "ServicesStartupTypeConfirmation",
                        service.DisplayName,
                        service.ServiceName,
                        GetStartupTypeText(
                            previousStartupType),
                        GetStartupTypeText(
                            selectedStartupType)),
                    LocalizationHelper.Get(
                        "WindowsUpdateYes"),
                    LocalizationHelper.Get(
                        "WindowsUpdateNo"));

            if (!confirmed)
            {
                service.CancelStartupTypeChange();

                return false;
            }

            service.IsBusy = true;

            try
            {
                ServiceOperationResult result =
                    await _startupManager
                        .SetStartupTypeAsync(
                            service.ServiceName,
                            selectedStartupType);

                if (!result.IsSuccessful)
                {
                    service.CancelStartupTypeChange();

                    MessageBox.Show(
                        LocalizationHelper.Format(
                            "ServicesStartupTypeApplyFailedMessage",
                            service.DisplayName),
                        LocalizationHelper.Get(
                            "ServicesStartupTypeApplyFailedTitle"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return false;
                }

                service.ConfirmStartupTypeChange();

                MessageBox.Show(
                    LocalizationHelper.Format(
                        "ServicesStartupTypeUpdatedMessage",
                        service.DisplayName,
                        GetStartupTypeText(
                            service.StartType)),
                    LocalizationHelper.Get(
                        "ServicesStartupTypeUpdatedTitle"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return true;
            }
            catch (Exception)
            {
                service.CancelStartupTypeChange();

                MessageBox.Show(
                    LocalizationHelper.Format(
                        "ServicesStartupTypeApplyFailedMessage",
                        service.DisplayName),
                    LocalizationHelper.Get(
                        "ServicesStartupTypeApplyFailedTitle"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return false;
            }
            finally
            {
                service.IsBusy = false;
            }
        }

        private static string GetStartupTypeText(
            string startupType)
        {
            return startupType switch
            {
                WindowsServiceInfo.StartupAutomatic =>
                    LocalizationHelper.Get(
                        "ServicesStartupAutomatic"),

                WindowsServiceInfo.StartupAutomaticDelayed =>
                    LocalizationHelper.Get(
                        "ServicesStartupAutomaticDelayed"),

                WindowsServiceInfo.StartupManual =>
                    LocalizationHelper.Get(
                        "ServicesStartupManual"),

                WindowsServiceInfo.StartupDisabled =>
                    LocalizationHelper.Get(
                        "ServicesStartupDisabled"),

                _ =>
                    startupType
            };
        }
    }
}