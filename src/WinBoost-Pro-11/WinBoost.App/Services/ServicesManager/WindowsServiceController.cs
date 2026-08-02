using System;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace WinBoost.App.Services.ServicesManager
{
    public class WindowsServiceController
    {
        private static readonly TimeSpan OperationTimeout =
            TimeSpan.FromSeconds(15);

        public Task<ServiceOperationResult> StartServiceAsync(
            string serviceName)
        {
            return Task.Run(() =>
            {
                try
                {
                    using var service =
                        new ServiceController(serviceName);

                    service.Refresh();

                    if (service.Status ==
                        ServiceControllerStatus.Running)
                    {
                        return ServiceOperationResult.Success(
                            "Serviciul este deja pornit.",
                            service.Status.ToString());
                    }

                    if (service.Status ==
                        ServiceControllerStatus.StartPending)
                    {
                        service.WaitForStatus(
                            ServiceControllerStatus.Running,
                            OperationTimeout);

                        service.Refresh();

                        return ServiceOperationResult.Success(
                            "Serviciul a fost pornit.",
                            service.Status.ToString());
                    }

                    service.Start();

                    service.WaitForStatus(
                        ServiceControllerStatus.Running,
                        OperationTimeout);

                    service.Refresh();

                    return ServiceOperationResult.Success(
                        "Serviciul a fost pornit.",
                        service.Status.ToString());
                }
                catch (InvalidOperationException ex)
                {
                    return ServiceOperationResult.Failure(
                        GetFriendlyErrorMessage(ex));
                }
                catch (System.ComponentModel.Win32Exception)
                {
                    return ServiceOperationResult.Failure(
                        "Operația necesită drepturi de administrator.");
                }
                catch (Exception ex)
                {
                    return ServiceOperationResult.Failure(
                        $"Serviciul nu a putut fi pornit: {ex.Message}");
                }
            });
        }

        public Task<ServiceOperationResult> StopServiceAsync(
            string serviceName)
        {
            return Task.Run(() =>
            {
                try
                {
                    using var service =
                        new ServiceController(serviceName);

                    service.Refresh();

                    if (service.Status ==
                        ServiceControllerStatus.Stopped)
                    {
                        return ServiceOperationResult.Success(
                            "Serviciul este deja oprit.",
                            service.Status.ToString());
                    }

                    if (!service.CanStop)
                    {
                        return ServiceOperationResult.Failure(
                            "Acest serviciu nu permite oprirea.");
                    }

                    if (service.Status ==
                        ServiceControllerStatus.StopPending)
                    {
                        service.WaitForStatus(
                            ServiceControllerStatus.Stopped,
                            OperationTimeout);

                        service.Refresh();

                        return ServiceOperationResult.Success(
                            "Serviciul a fost oprit.",
                            service.Status.ToString());
                    }

                    service.Stop();

                    service.WaitForStatus(
                        ServiceControllerStatus.Stopped,
                        OperationTimeout);

                    service.Refresh();

                    return ServiceOperationResult.Success(
                        "Serviciul a fost oprit.",
                        service.Status.ToString());
                }
                catch (InvalidOperationException ex)
                {
                    return ServiceOperationResult.Failure(
                        GetFriendlyErrorMessage(ex));
                }
                catch (System.ComponentModel.Win32Exception)
                {
                    return ServiceOperationResult.Failure(
                        "Operația necesită drepturi de administrator.");
                }
                catch (Exception ex)
                {
                    return ServiceOperationResult.Failure(
                        $"Serviciul nu a putut fi oprit: {ex.Message}");
                }
            });
        }

        public async Task<ServiceOperationResult> RestartServiceAsync(
            string serviceName)
        {
            ServiceOperationResult stopResult =
                await StopServiceAsync(serviceName);

            if (!stopResult.IsSuccessful)
            {
                return stopResult;
            }

            ServiceOperationResult startResult =
                await StartServiceAsync(serviceName);

            if (!startResult.IsSuccessful)
            {
                return startResult;
            }

            return ServiceOperationResult.Success(
                "Serviciul a fost repornit.",
                startResult.CurrentStatus);
        }

        public Task<ServiceOperationResult> GetServiceStatusAsync(
            string serviceName)
        {
            return Task.Run(() =>
            {
                try
                {
                    using var service =
                        new ServiceController(serviceName);

                    service.Refresh();

                    return ServiceOperationResult.Success(
                        "Starea serviciului a fost actualizată.",
                        service.Status.ToString());
                }
                catch (InvalidOperationException ex)
                {
                    return ServiceOperationResult.Failure(
                        GetFriendlyErrorMessage(ex));
                }
                catch (Exception ex)
                {
                    return ServiceOperationResult.Failure(
                        $"Starea serviciului nu a putut fi citită: " +
                        $"{ex.Message}");
                }
            });
        }

        private static string GetFriendlyErrorMessage(
            InvalidOperationException exception)
        {
            if (exception.InnerException is
                System.ComponentModel.Win32Exception)
            {
                return
                    "Windows a refuzat operația. " +
                    "Pornește WinBoost ca administrator.";
            }

            return exception.Message;
        }
    }

    public class ServiceOperationResult
    {
        public bool IsSuccessful { get; init; }

        public string Message { get; init; } =
            string.Empty;

        public string CurrentStatus { get; init; } =
            string.Empty;

        public static ServiceOperationResult Success(
            string message,
            string currentStatus = "")
        {
            return new ServiceOperationResult
            {
                IsSuccessful = true,
                Message = message,
                CurrentStatus = currentStatus
            };
        }

        public static ServiceOperationResult Failure(
            string message)
        {
            return new ServiceOperationResult
            {
                IsSuccessful = false,
                Message = message
            };
        }
    }
}