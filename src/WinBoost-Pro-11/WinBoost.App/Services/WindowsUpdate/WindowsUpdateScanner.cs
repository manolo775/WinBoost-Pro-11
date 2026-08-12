using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace WinBoost.App.Services.WindowsUpdate
{
    public sealed class WindowsUpdateScanResult
    {
        public int CheckedServices
        {
            get;
            init;
        }

        public int RunningServices
        {
            get;
            init;
        }

        public IReadOnlyList<string> StoppedServices
        {
            get;
            init;
        } = Array.Empty<string>();

        public IReadOnlyList<string> DisabledServices
        {
            get;
            init;
        } = Array.Empty<string>();
    }

    public class WindowsUpdateScanner
    {
        private static readonly Dictionary<string, string>
            RequiredServices =
                new(StringComparer.OrdinalIgnoreCase)
                {
                    {
                        "wuauserv",
                        "Windows Update"
                    },
                    {
                        "BITS",
                        "Background Intelligent Transfer Service"
                    },
                    {
                        "UsoSvc",
                        "Update Orchestrator Service"
                    },
                    {
                        "DoSvc",
                        "Delivery Optimization"
                    }
                };

        public Task<WindowsUpdateScanResult> ScanAsync()
        {
            return Task.Run(
                () =>
                {
                    ServiceController[] services =
                        ServiceController.GetServices();

                    try
                    {
                        int availableServices =
                            0;

                        var stoppedServices =
                            new List<string>();

                        var disabledServices =
                            new List<string>();

                        foreach (
                            var requiredService
                            in RequiredServices)
                        {
                            ServiceController? service =
                                services.FirstOrDefault(
                                    item =>
                                        string.Equals(
                                            item.ServiceName,
                                            requiredService.Key,
                                            StringComparison.OrdinalIgnoreCase));

                            if (service == null)
                            {
                                stoppedServices.Add(
                                    $"{requiredService.Value} nu a fost găsit");

                                continue;
                            }

                            ServiceStartMode? startType =
                                null;

                            try
                            {
                                startType =
                                    service.StartType;
                            }
                            catch
                            {
                                // Dacă StartType nu poate fi citit,
                                // nu presupunem automat că serviciul
                                // este defect.
                            }

                            if (startType ==
                                ServiceStartMode.Disabled)
                            {
                                disabledServices.Add(
                                    requiredService.Value);

                                continue;
                            }

                            /*
                             * Pe Windows 11, servicii precum
                             * Windows Update pot fi Manual /
                             * Trigger Start și pot apărea Stopped
                             * atunci când nu au activitate.
                             *
                             * Dacă serviciul există și nu este
                             * Disabled, îl considerăm disponibil.
                             */
                            availableServices++;

                            /*
                             * Păstrăm StoppedServices doar pentru
                             * stări tranzitorii/problematic-neclare.
                             */
                            if (service.Status ==
                                    ServiceControllerStatus.StopPending ||
                                service.Status ==
                                    ServiceControllerStatus.PausePending ||
                                service.Status ==
                                    ServiceControllerStatus.Paused)
                            {
                                stoppedServices.Add(
                                    requiredService.Value);
                            }
                        }

                        return new WindowsUpdateScanResult
                        {
                            CheckedServices =
                                RequiredServices.Count,

                            RunningServices =
                                availableServices,

                            StoppedServices =
                                stoppedServices,

                            DisabledServices =
                                disabledServices
                        };
                    }
                    finally
                    {
                        foreach (
                            ServiceController service
                            in services)
                        {
                            service.Dispose();
                        }
                    }
                });
        }
    }
}