using System;
using System.Threading.Tasks;
using System.Windows;
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
                    "WinBoost nu permite schimbarea tipului de pornire " +
                    "pentru acest serviciu critic.",
                    "Operație blocată",
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

            MessageBoxResult confirmation =
                MessageBox.Show(
                    $"Dorești să schimbi tipul de pornire pentru:\n\n" +
                    $"{service.DisplayName}\n" +
                    $"({service.ServiceName})\n\n" +
                    $"Din: {previousStartupType}\n" +
                    $"În: {selectedStartupType}",
                    "Confirmare tip pornire",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

            if (confirmation != MessageBoxResult.Yes)
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
                        result.Message,
                        "Modificarea nu a putut fi aplicată",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return false;
                }

                service.ConfirmStartupTypeChange();

                MessageBox.Show(
                    result.Message,
                    "Tip de pornire actualizat",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return true;
            }
            catch (Exception ex)
            {
                service.CancelStartupTypeChange();

                MessageBox.Show(
                    $"Tipul de pornire nu a putut fi schimbat:\n\n" +
                    ex.Message,
                    "Eroare",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return false;
            }
            finally
            {
                service.IsBusy = false;
            }
        }
    }
}