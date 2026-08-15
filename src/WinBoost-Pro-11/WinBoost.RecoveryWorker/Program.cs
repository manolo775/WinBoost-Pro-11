using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Management;
using System.Text.Json;

namespace WinBoost.RecoveryWorker
{
    internal class Program
    {
        private const int SuccessExitCode = 0;
        private const int InvalidArgumentsExitCode = 10;
        private const int OperationFailedExitCode = 20;
        private const int UnexpectedErrorExitCode = 100;

        private static int Main(
            string[] args)
        {
            try
            {
                if (args.Length == 0)
                {
                    return InvalidArgumentsExitCode;
                }

                string command =
                    args[0].Trim();

                if (string.Equals(
                        command,
                        "--create",
                        StringComparison.OrdinalIgnoreCase))
                {
                    string description =
                        ParseArgumentValue(
                            args,
                            "--description=");

                    if (string.IsNullOrWhiteSpace(
                            description))
                    {
                        description =
                            $"WinBoost Restore Point " +
                            $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                    }

                    return CreateRestorePoint(
                        description);
                }

                if (string.Equals(
                 command,
                 "--list",
                 StringComparison.OrdinalIgnoreCase))
                {
                    string outputPath =
                        ParseArgumentValue(
                            args,
                            "--output=");

                    if (string.IsNullOrWhiteSpace(
                            outputPath))
                    {
                        return InvalidArgumentsExitCode;
                    }

                    return WriteRestorePointList(
                        outputPath);
                }

                if (string.Equals(
                        command,
                        "--restore",
                        StringComparison.OrdinalIgnoreCase))
                {
                    string sequenceNumberText =
                        ParseArgumentValue(
                            args,
                            "--sequence-number=");

                    if (!uint.TryParse(
                            sequenceNumberText,
                            NumberStyles.Integer,
                            CultureInfo.InvariantCulture,
                            out uint sequenceNumber))
                    {
                        return InvalidArgumentsExitCode;
                    }

                    return RestoreSystem(
                        sequenceNumber);
                }

                return InvalidArgumentsExitCode;
            }
            catch
            {
                return UnexpectedErrorExitCode;
            }
        }

        private static int CreateRestorePoint(
     string description)
        {
            HashSet<uint> restorePointsBefore =
                GetRestorePointSequenceNumbers();

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

            var path =
                new ManagementPath(
                    "SystemRestore");

            using var restoreClass =
                new ManagementClass(
                    scope,
                    path,
                    null);

            using ManagementBaseObject?
                inputParameters =
                    restoreClass
                        .GetMethodParameters(
                            "CreateRestorePoint");

            if (inputParameters == null)
            {
                return OperationFailedExitCode;
            }

            inputParameters[
                "Description"] =
                    description;

            // MODIFY_SETTINGS
            inputParameters[
                "RestorePointType"] =
                    12;

            // BEGIN_SYSTEM_CHANGE
            inputParameters[
                "EventType"] =
                    100;

            using ManagementBaseObject?
                outputParameters =
                    restoreClass
                        .InvokeMethod(
                            "CreateRestorePoint",
                            inputParameters,
                            null);

            uint returnCode =
                Convert.ToUInt32(
                    outputParameters?[
                        "ReturnValue"]
                    ?? 1u);

            if (returnCode != 0)
            {
                return OperationFailedExitCode;
            }

            /*
             * Windows poate întoarce cod 0 fără să creeze
             * efectiv un Restore Point nou.
             *
             * Verificăm până la aproximativ 10 secunde
             * dacă apare un SequenceNumber nou.
             */

            for (int attempt = 0;
                 attempt < 10;
                 attempt++)
            {
                System.Threading.Thread.Sleep(
                    TimeSpan.FromSeconds(1));

                HashSet<uint> restorePointsAfter =
                    GetRestorePointSequenceNumbers();

                foreach (uint sequenceNumber
                         in restorePointsAfter)
                {
                    if (!restorePointsBefore.Contains(
                            sequenceNumber))
                    {
                        return SuccessExitCode;
                    }
                }
            }

            /*
             * WMI a spus succes, dar Windows nu a creat
             * un Restore Point nou.
             */

            return 21;
        }

        private static HashSet<uint>
    GetRestorePointSequenceNumbers()
        {
            var sequenceNumbers =
                new HashSet<uint>();

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
                    "SELECT SequenceNumber FROM SystemRestore");

            using var searcher =
                new ManagementObjectSearcher(
                    scope,
                    query);

            using ManagementObjectCollection
                results =
                    searcher.Get();

            foreach (ManagementObject restorePoint
                     in results)
            {
                object? sequenceValue =
                    restorePoint[
                        "SequenceNumber"];

                if (sequenceValue == null)
                {
                    continue;
                }

                uint sequenceNumber =
                    Convert.ToUInt32(
                        sequenceValue);

                sequenceNumbers.Add(
                    sequenceNumber);
            }

            return sequenceNumbers;
        }

        private static int RestoreSystem(
    uint sequenceNumber)
        {
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

            var path =
                new ManagementPath(
                    "SystemRestore");

            using var restoreClass =
                new ManagementClass(
                    scope,
                    path,
                    null);

            using ManagementBaseObject?
                inputParameters =
                    restoreClass
                        .GetMethodParameters(
                            "Restore");

            if (inputParameters == null)
            {
                return OperationFailedExitCode;
            }

            inputParameters[
                "SequenceNumber"] =
                    sequenceNumber;

            using ManagementBaseObject?
                outputParameters =
                    restoreClass
                        .InvokeMethod(
                            "Restore",
                            inputParameters,
                            null);

            uint returnCode =
                Convert.ToUInt32(
                    outputParameters?[
                        "ReturnValue"]
                    ?? 1u);

            return returnCode == 0
                ? SuccessExitCode
                : OperationFailedExitCode;
        }

        private static int WriteRestorePointList(
            string outputPath)
        {
            var restorePoints =
                new List<RestorePointWorkerItem>();

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
                    new RestorePointWorkerItem
                    {
                        SequenceNumber =
                            sequenceNumber,

                        Description =
                            description,

                        RestorePointType =
                            restorePointType,

                        CreatedAt =
                            createdAt
                    });
            }

            restorePoints.Sort(
                (left, right) =>
                    right.CreatedAt.CompareTo(
                        left.CreatedAt));

            string? directory =
                Path.GetDirectoryName(
                    outputPath);

            if (!string.IsNullOrWhiteSpace(
                    directory))
            {
                Directory.CreateDirectory(
                    directory);
            }

            string json =
                JsonSerializer.Serialize(
                    restorePoints,
                    new JsonSerializerOptions
                    {
                        WriteIndented =
                            true
                    });

            File.WriteAllText(
                outputPath,
                json);

            return SuccessExitCode;
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

        private static string ParseArgumentValue(
            string[] args,
            string prefix)
        {
            foreach (string argument
                     in args)
            {
                if (!argument.StartsWith(
                        prefix,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return argument
                    .Substring(
                        prefix.Length)
                    .Trim();
            }

            return string.Empty;
        }

        private sealed class RestorePointWorkerItem
        {
            public uint SequenceNumber
            {
                get;
                init;
            }

            public string Description
            {
                get;
                init;
            } = string.Empty;

            public uint RestorePointType
            {
                get;
                init;
            }

            public DateTime CreatedAt
            {
                get;
                init;
            }
        }
    }
}