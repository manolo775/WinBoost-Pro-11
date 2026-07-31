using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;
using System.Windows.Media;
using WinBoost.App.Models;

namespace WinBoost.App.Services.ServicesManager
{
    public class WindowsServiceScanner
    {
        private static readonly HashSet<string>
            MonitoredServiceNames =
                new(StringComparer.OrdinalIgnoreCase)
                {
                    "SysMain",
                    "WSearch",
                    "DiagTrack",
                    "DoSvc",
                    "WerSvc",
                    "MapsBroker",
                    "RemoteRegistry",
                    "Fax",
                    "XblAuthManager",
                    "XblGameSave",
                    "XboxNetApiSvc"
                };

        public Task<List<WindowsServiceInfo>> ScanAsync()
        {
            return Task.Run(() =>
            {
                ServiceController[] services =
                    ServiceController.GetServices();

                try
                {
                    return services
                        .Where(service =>
                            MonitoredServiceNames.Contains(
                                service.ServiceName))
                        .OrderBy(service => service.DisplayName)
                        .Select(CreateServiceInfo)
                        .ToList();
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

        private static WindowsServiceInfo CreateServiceInfo(
            ServiceController service)
        {
            string startType;

            try
            {
                startType =
                    TranslateStartType(service.StartType);
            }
            catch
            {
                startType = "Necunoscut";
            }

            return new WindowsServiceInfo
            {
                DisplayName = service.DisplayName,
                ServiceName = service.ServiceName,
                Status = TranslateStatus(service.Status),
                StartType = startType,
                Recommendation =
                    GetRecommendation(
                        service.Status,
                        startType),
                StatusBrush =
                    GetStatusBrush(service.Status)
            };
        }

        private static string TranslateStatus(
            ServiceControllerStatus status)
        {
            return status switch
            {
                ServiceControllerStatus.Running =>
                    "Rulează",

                ServiceControllerStatus.Stopped =>
                    "Oprit",

                ServiceControllerStatus.Paused =>
                    "În pauză",

                ServiceControllerStatus.StartPending =>
                    "Se pornește",

                ServiceControllerStatus.StopPending =>
                    "Se oprește",

                ServiceControllerStatus.PausePending =>
                    "Intră în pauză",

                ServiceControllerStatus.ContinuePending =>
                    "Se reia",

                _ => "Necunoscut"
            };
        }

        private static string TranslateStartType(
            ServiceStartMode startType)
        {
            return startType switch
            {
                ServiceStartMode.Automatic =>
                    "Automat",

                ServiceStartMode.Manual =>
                    "Manual",

                ServiceStartMode.Disabled =>
                    "Dezactivat",

                ServiceStartMode.Boot =>
                    "Pornire sistem",

                ServiceStartMode.System =>
                    "Sistem",

                _ => "Necunoscut"
            };
        }

        private static string GetRecommendation(
            ServiceControllerStatus status,
            string startType)
        {
            if (startType == "Automat" &&
                status == ServiceControllerStatus.Stopped)
            {
                return
                    "Verifică serviciul: este setat automat, dar este oprit.";
            }

            if (startType == "Dezactivat")
            {
                return
                    "Serviciul este dezactivat în Windows.";
            }

            if (status == ServiceControllerStatus.Running)
            {
                return "Funcționează normal.";
            }

            return
                "Nu este necesară nicio acțiune imediată.";
        }

        private static Brush GetStatusBrush(
            ServiceControllerStatus status)
        {
            return status switch
            {
                ServiceControllerStatus.Running =>
                    Brushes.LimeGreen,

                ServiceControllerStatus.Stopped =>
                    Brushes.Orange,

                ServiceControllerStatus.Paused =>
                    Brushes.Gold,

                _ => Brushes.LightGray
            };
        }
    }
}