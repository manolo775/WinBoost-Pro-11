using System;
using System.Collections.Generic;
using System.Globalization;
using System.Management;
using System.Threading.Tasks;
using WinBoost.App.Models;

namespace WinBoost.App.Services.Recovery
{
    public sealed class SystemRestorePointScanner
    {
        public Task<IReadOnlyList<SystemRestorePointInfo>>
            ScanAsync()
        {
            return Task.Run(
                Scan);
        }

        private static IReadOnlyList<SystemRestorePointInfo>
            Scan()
        {
            var restorePoints =
                new List<SystemRestorePointInfo>();

            var connectionOptions =
                new ConnectionOptions
                {
                    Impersonation =
                        ImpersonationLevel.Impersonate,

                    EnablePrivileges =
                        true
                };

            var scope =
                new ManagementScope(
                    @"\\.\root\default",
                    connectionOptions);

            scope.Connect();

            var query =
                new ObjectQuery(
                    "SELECT * FROM SystemRestore");

            using var searcher =
                new ManagementObjectSearcher(
                    scope,
                    query);

            using ManagementObjectCollection
                results =
                    searcher.Get();

            foreach (ManagementObject item
                     in results)
            {
                uint sequenceNumber =
                    Convert.ToUInt32(
                        item["SequenceNumber"]
                        ?? 0u);

                string description =
                    Convert.ToString(
                        item["Description"],
                        CultureInfo.InvariantCulture)
                    ?? string.Empty;

                uint restorePointType =
                    Convert.ToUInt32(
                        item["RestorePointType"]
                        ?? 0u);

                string creationTime =
                    Convert.ToString(
                        item["CreationTime"],
                        CultureInfo.InvariantCulture)
                    ?? string.Empty;

                DateTime createdAt =
                    ParseCreationTime(
                        creationTime);

                restorePoints.Add(
                    new SystemRestorePointInfo
                    {
                        SequenceNumber =
                            sequenceNumber,

                        Description =
                            description,

                        RestorePointType =
                            restorePointType,

                        RestorePointTypeName =
                            GetRestorePointTypeName(
                                restorePointType),

                        CreatedAt =
                            createdAt
                    });
            }

            restorePoints.Sort(
                (left, right) =>
                    right.CreatedAt.CompareTo(
                        left.CreatedAt));

            return restorePoints;
        }

        private static DateTime ParseCreationTime(
            string creationTime)
        {
            if (string.IsNullOrWhiteSpace(
                    creationTime))
            {
                return DateTime.MinValue;
            }

            try
            {
                return ManagementDateTimeConverter
                    .ToDateTime(
                        creationTime);
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        private static string GetRestorePointTypeName(
            uint restorePointType)
        {
            return restorePointType switch
            {
                0 =>
                    "Application Install",

                1 =>
                    "Application Uninstall",

                10 =>
                    "Device Driver Install",

                12 =>
                    "System",

                13 =>
                    "Cancelled Operation",

                _ =>
                    "Other"
            };
        }
    }
}