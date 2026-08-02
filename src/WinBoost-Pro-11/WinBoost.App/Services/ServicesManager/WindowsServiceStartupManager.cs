using Microsoft.Win32;
using System;
using System.Threading.Tasks;

namespace WinBoost.App.Services.ServicesManager
{
    public class WindowsServiceStartupManager
    {
        private const string ServicesRegistryPath =
            @"SYSTEM\CurrentControlSet\Services";

        public Task<ServiceOperationResult> SetStartupTypeAsync(
            string serviceName,
            string startupType)
        {
            return Task.Run(() =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(serviceName))
                    {
                        return ServiceOperationResult.Failure(
                            "Numele serviciului nu este valid.");
                    }

                    int startValue =
                        GetStartValue(startupType);

                    using RegistryKey? serviceKey =
                        Registry.LocalMachine.OpenSubKey(
                            $@"{ServicesRegistryPath}\{serviceName}",
                            writable: true);

                    if (serviceKey == null)
                    {
                        return ServiceOperationResult.Failure(
                            "Serviciul nu a fost găsit în Registry.");
                    }

                    serviceKey.SetValue(
                        "Start",
                        startValue,
                        RegistryValueKind.DWord);

                    if (startupType.Equals(
                            "Automatic (Delayed)",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        serviceKey.SetValue(
                            "DelayedAutoStart",
                            1,
                            RegistryValueKind.DWord);
                    }
                    else
                    {
                        serviceKey.SetValue(
                            "DelayedAutoStart",
                            0,
                            RegistryValueKind.DWord);
                    }

                    return ServiceOperationResult.Success(
                        $"Tipul de pornire a fost schimbat în " +
                        $"{startupType}.");
                }
                catch (UnauthorizedAccessException)
                {
                    return ServiceOperationResult.Failure(
                        "Windows a refuzat accesul. " +
                        "Pornește WinBoost ca administrator.");
                }
                catch (System.Security.SecurityException)
                {
                    return ServiceOperationResult.Failure(
                        "Operația necesită drepturi de administrator.");
                }
                catch (Exception ex)
                {
                    return ServiceOperationResult.Failure(
                        "Tipul de pornire nu a putut fi schimbat: " +
                        ex.Message);
                }
            });
        }

        private static int GetStartValue(
            string startupType)
        {
            return startupType switch
            {
                "Automatic" => 2,
                "Automatic (Delayed)" => 2,
                "Manual" => 3,
                "Disabled" => 4,

                _ => throw new ArgumentException(
                    "Tipul de pornire selectat nu este valid.",
                    nameof(startupType))
            };
        }
    }
}