using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;
using System.Windows.Media;
using WinBoost.App.Models;

namespace WinBoost.App.Services.ServicesManager
{
    public class WindowsServiceManager
    {
        private const string ServicesRegistryPath =
            @"SYSTEM\CurrentControlSet\Services";

        private readonly WindowsServiceRecommendationEngine
            _recommendationEngine;

        public WindowsServiceManager()
        {
            _recommendationEngine =
                new WindowsServiceRecommendationEngine();
        }

        public Task<List<WindowsServiceInfo>> GetServicesAsync()
        {
            return Task.Run(() =>
            {
                var services =
                    new List<WindowsServiceInfo>();

                foreach (ServiceController service
                         in ServiceController
                             .GetServices()
                             .OrderBy(item =>
                                 item.DisplayName))
                {
                    try
                    {
                        string serviceName =
                            service.ServiceName;

                        services.Add(
                            new WindowsServiceInfo
                            {
                                DisplayName =
                                    string.IsNullOrWhiteSpace(
                                        service.DisplayName)
                                        ? serviceName
                                        : service.DisplayName,

                                ServiceName =
                                    serviceName,

                                Status =
                                    service.Status.ToString(),

                                StartType =
                                    GetStartType(
                                        serviceName),

                                Recommendation =
                                    _recommendationEngine
                                        .GetRecommendation(
                                            serviceName),

                                IsCritical =
                                    _recommendationEngine
                                        .IsCritical(
                                            serviceName),

                                RiskLevel =
                                    _recommendationEngine
                                        .GetRiskLevel(
                                            serviceName),

                                CanBeStoppedSafely =
                                    _recommendationEngine
                                        .CanBeStoppedSafely(
                                            serviceName),

                                StatusBrush =
                                    GetStatusBrush(
                                        service.Status)
                            });
                    }
                    catch
                    {
                        // Dacă un serviciu nu poate fi citit complet,
                        // continuăm cu următorul.
                    }
                    finally
                    {
                        service.Dispose();
                    }
                }

                return services;
            });
        }

        private static string GetStartType(
            string serviceName)
        {
            try
            {
                using RegistryKey? serviceKey =
                    Registry.LocalMachine.OpenSubKey(
                        $@"{ServicesRegistryPath}\{serviceName}");

                if (serviceKey == null)
                {
                    return "Unknown";
                }

                object? startValue =
                    serviceKey.GetValue("Start");

                if (startValue == null)
                {
                    return "Unknown";
                }

                int startType =
                    Convert.ToInt32(startValue);

                bool delayedAutomatic =
                    Convert.ToInt32(
                        serviceKey.GetValue(
                            "DelayedAutoStart",
                            0)) == 1;

                return startType switch
                {
                    0 => "Boot",
                    1 => "System",

                    2 when delayedAutomatic =>
                        "Automatic (Delayed)",

                    2 => "Automatic",
                    3 => "Manual",
                    4 => "Disabled",

                    _ => "Unknown"
                };
            }
            catch
            {
                return "Unknown";
            }
        }

        private static Brush GetStatusBrush(
            ServiceControllerStatus status)
        {
            return status ==
                   ServiceControllerStatus.Running
                ? Brushes.LimeGreen
                : Brushes.Orange;
        }
    }
}