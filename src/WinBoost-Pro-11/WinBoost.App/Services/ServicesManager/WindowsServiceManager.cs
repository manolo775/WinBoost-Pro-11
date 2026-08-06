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
    public sealed class WindowsServiceManager
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

                IEnumerable<ServiceController>
                    orderedServices =
                        ServiceController
                            .GetServices()
                            .OrderBy(
                                service =>
                                    service.DisplayName,
                                StringComparer
                                    .CurrentCultureIgnoreCase);

                foreach (ServiceController service
                         in orderedServices)
                {
                    try
                    {
                        string serviceName =
                            service.ServiceName;

                        string displayName =
                            string.IsNullOrWhiteSpace(
                                service.DisplayName)
                                ? serviceName
                                : service.DisplayName;

                        string status =
                            GetStatus(
                                service.Status);

                        string startType =
                            GetStartType(
                                serviceName);

                        services.Add(
                            new WindowsServiceInfo
                            {
                                DisplayName =
                                    displayName,

                                ServiceName =
                                    serviceName,

                                Status =
                                    status,

                                StartType =
                                    startType,

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
                                        status)
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

        private static string GetStatus(
            ServiceControllerStatus status)
        {
            return status ==
                   ServiceControllerStatus.Running
                ? WindowsServiceInfo.StatusRunning
                : WindowsServiceInfo.StatusStopped;
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
                    return WindowsServiceInfo.StartupManual;
                }

                object? startValue =
                    serviceKey.GetValue(
                        "Start");

                if (startValue == null)
                {
                    return WindowsServiceInfo.StartupManual;
                }

                int startType =
                    Convert.ToInt32(
                        startValue);

                bool delayedAutomatic =
                    Convert.ToInt32(
                        serviceKey.GetValue(
                            "DelayedAutoStart",
                            0)) == 1;

                return startType switch
                {
                    2 when delayedAutomatic =>
                        WindowsServiceInfo
                            .StartupAutomaticDelayed,

                    2 =>
                        WindowsServiceInfo
                            .StartupAutomatic,

                    4 =>
                        WindowsServiceInfo
                            .StartupDisabled,

                    _ =>
                        WindowsServiceInfo
                            .StartupManual
                };
            }
            catch
            {
                return WindowsServiceInfo.StartupManual;
            }
        }

        private static Brush GetStatusBrush(
            string status)
        {
            return status.Equals(
                WindowsServiceInfo.StatusRunning,
                StringComparison.OrdinalIgnoreCase)
                ? Brushes.LimeGreen
                : Brushes.Orange;
        }
    }
}