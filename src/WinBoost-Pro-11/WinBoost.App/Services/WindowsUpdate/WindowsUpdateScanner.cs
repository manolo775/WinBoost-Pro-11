using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace WinBoost.App.Services.WindowsUpdate
{
    public sealed class WindowsUpdateScanResult
    {
        public int CheckedServices { get; init; }

        public int RunningServices { get; init; }

        public IReadOnlyList<string> StoppedServices { get; init; } =
            Array.Empty<string>();

        public IReadOnlyList<string> DisabledServices { get; init; } =
            Array.Empty<string>();
    }

    public class WindowsUpdateScanner
    {
        private static readonly Dictionary<string, string>
            RequiredServices =
                new(StringComparer.OrdinalIgnoreCase)
                {
                    { "wuauserv", "Windows Update" },
                    { "BITS", "Background Intelligent Transfer Service" },
                    { "UsoSvc", "Update Orchestrator Service" },
                    { "DoSvc", "Delivery Optimization" }
                };

        public Task<WindowsUpdateScanResult> ScanAsync()
        {
            return Task.Run(() =>
            {
                ServiceController[] services =
                    ServiceController.GetServices();

                try
                {
                    int runningServices = 0;

                    var stoppedServices =
                        new List<string>();

                    var disabledServices =
                        new List<string>();

                    foreach (var requiredService in RequiredServices)
                    {
                        ServiceController? service =
                            services.FirstOrDefault(item =>
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

                        if (service.Status ==
                            ServiceControllerStatus.Running)
                        {
                            runningServices++;
                        }
                        else
                        {
                            stoppedServices.Add(
                                requiredService.Value);
                        }

                        try
                        {
                            if (service.StartType ==
                                ServiceStartMode.Disabled)
                            {
                                disabledServices.Add(
                                    requiredService.Value);
                            }
                        }
                        catch
                        {
                            // Unele informații pot fi inaccesibile
                            // fără drepturi administrative.
                        }
                    }

                    return new WindowsUpdateScanResult
                    {
                        CheckedServices =
                            RequiredServices.Count,

                        RunningServices =
                            runningServices,

                        StoppedServices =
                            stoppedServices,

                        DisabledServices =
                            disabledServices
                    };
                }
                finally
                {
                    foreach (ServiceController service in services)
                    {
                        service.Dispose();
                    }
                }
            });
        }
    }
}