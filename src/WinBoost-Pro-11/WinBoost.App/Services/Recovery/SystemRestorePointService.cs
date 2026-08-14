using System;
using System.Collections.Generic;
using System.Management;
using System.Threading;
using System.Threading.Tasks;

namespace WinBoost.App.Services.Recovery
{
    public sealed class SystemRestorePointService
    {
        private const uint SuccessCode = 0;

        private const uint GenericErrorCode = 1;

        private const uint AccessDeniedCode = 5;

        private const int VerificationAttempts = 10;

        private static readonly TimeSpan
            VerificationDelay =
                TimeSpan.FromSeconds(1);


        // ======================================
        // CHECK AVAILABILITY
        // ======================================

        public Task<SystemRestoreAvailabilityResult>
            CheckAvailabilityAsync()
        {
            return Task.Run(
                CheckAvailability);
        }


        // ======================================
        // CREATE RESTORE POINT
        // ======================================

        public async Task<SystemRestorePointResult>
            CreateRestorePointAsync(
                string description)
        {
            try
            {
                HashSet<uint>
                    restorePointsBefore =
                        await Task.Run(
                            GetRestorePointSequenceNumbers);

                SystemRestorePointResult
                    creationResult =
                        await Task.Run(
                            () =>
                                CreateRestorePoint(
                                    description));

                if (!creationResult.IsSuccessful)
                {
                    return creationResult;
                }

                /*
                 * Windows poate întoarce cod 0 chiar dacă
                 * noul restore point nu apare imediat.
                 *
                 * Verificăm câteva secunde dacă a fost
                 * creat efectiv un SequenceNumber nou.
                 */

                for (int attempt = 0;
                     attempt < VerificationAttempts;
                     attempt++)
                {
                    await Task.Delay(
                        VerificationDelay);

                    HashSet<uint>
                        restorePointsAfter =
                            await Task.Run(
                                GetRestorePointSequenceNumbers);

                    foreach (uint sequenceNumber
                             in restorePointsAfter)
                    {
                        if (!restorePointsBefore.Contains(
                                sequenceNumber))
                        {
                            return new SystemRestorePointResult
                            {
                                IsSuccessful =
                                    true,

                                ReturnCode =
                                    SuccessCode,

                                Message =
                                    "Restore point created successfully."
                            };
                        }
                    }
                }

                /*
                 * WMI a returnat succes, dar lista
                 * SystemRestore nu conține un punct nou.
                 *
                 * Nu raportăm fals succes utilizatorului.
                 */

                return new SystemRestorePointResult
                {
                    IsSuccessful =
                        false,

                    ReturnCode =
                        SuccessCode,

                    Message =
                         "RECOVERY_NO_NEW_RESTORE_POINT"
                };
            }
            catch (UnauthorizedAccessException ex)
            {
                return new SystemRestorePointResult
                {
                    IsSuccessful =
                        false,

                    ReturnCode =
                        AccessDeniedCode,

                    Message =
                        BuildExceptionMessage(
                            "Access denied",
                            ex)
                };
            }
            catch (ManagementException ex)
            {
                return new SystemRestorePointResult
                {
                    IsSuccessful =
                        false,

                    ReturnCode =
                        GenericErrorCode,

                    Message =
                        BuildExceptionMessage(
                            "System Restore error",
                            ex)
                };
            }
            catch (Exception ex)
            {
                return new SystemRestorePointResult
                {
                    IsSuccessful =
                        false,

                    ReturnCode =
                        GenericErrorCode,

                    Message =
                        BuildExceptionMessage(
                            "Restore point error",
                            ex)
                };
            }
        }


        // ======================================
        // ENABLE SYSTEM PROTECTION
        // ======================================

        public Task<SystemProtectionResult>
            EnableSystemProtectionAsync()
        {
            return Task.Run(
                EnableSystemProtection);
        }


        // ======================================
        // RESTORE SYSTEM
        // ======================================

        public Task<SystemRestorePointResult>
            RestoreSystemAsync(
                uint sequenceNumber)
        {
            return Task.Run(
                () =>
                    RestoreSystem(
                        sequenceNumber));
        }


        // ======================================
        // MANAGEMENT SCOPE
        // ======================================

        private static ManagementScope
            CreateManagementScope()
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

            return scope;
        }


        // ======================================
        // CHECK SYSTEM RESTORE
        // ======================================

        private static SystemRestoreAvailabilityResult
            CheckAvailability()
        {
            try
            {
                ManagementScope scope =
                    CreateManagementScope();

                var path =
                    new ManagementPath(
                        "SystemRestore");

                using var restoreClass =
                    new ManagementClass(
                        scope,
                        path,
                        null);

                restoreClass.Get();

                return new SystemRestoreAvailabilityResult
                {
                    IsAvailable =
                        true,

                    Message =
                        "System Restore is available."
                };
            }
            catch (Exception ex)
            {
                return new SystemRestoreAvailabilityResult
                {
                    IsAvailable =
                        false,

                    Message =
                        BuildExceptionMessage(
                            "System Restore is unavailable",
                            ex)
                };
            }
        }


        // ======================================
        // READ EXISTING RESTORE POINT NUMBERS
        // ======================================

        private static HashSet<uint>
            GetRestorePointSequenceNumbers()
        {
            var sequenceNumbers =
                new HashSet<uint>();

            ManagementScope scope =
                CreateManagementScope();

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

            foreach (ManagementObject
                     restorePoint in results)
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


        // ======================================
        // CREATE RESTORE POINT - WMI
        // ======================================

        private static SystemRestorePointResult
            CreateRestorePoint(
                string description)
        {
            try
            {
                ManagementScope scope =
                    CreateManagementScope();

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
                    return new SystemRestorePointResult
                    {
                        IsSuccessful =
                            false,

                        ReturnCode =
                            GenericErrorCode,

                        Message =
                            "Restore point parameters could not be created."
                    };
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
                        ?? GenericErrorCode);

                if (returnCode !=
                    SuccessCode)
                {
                    return new SystemRestorePointResult
                    {
                        IsSuccessful =
                            false,

                        ReturnCode =
                            returnCode,

                        Message =
                            $"Restore point creation failed. " +
                            $"Code: {returnCode}"
                    };
                }

                /*
                 * Acesta este doar succesul apelului WMI.
                 * CreateRestorePointAsync verifică separat
                 * dacă punctul a apărut efectiv în Windows.
                 */

                return new SystemRestorePointResult
                {
                    IsSuccessful =
                        true,

                    ReturnCode =
                        SuccessCode,

                    Message =
                        "System Restore accepted the create request."
                };
            }
            catch (UnauthorizedAccessException ex)
            {
                return new SystemRestorePointResult
                {
                    IsSuccessful =
                        false,

                    ReturnCode =
                        AccessDeniedCode,

                    Message =
                        BuildExceptionMessage(
                            "Access denied",
                            ex)
                };
            }
            catch (ManagementException ex)
            {
                return new SystemRestorePointResult
                {
                    IsSuccessful =
                        false,

                    ReturnCode =
                        GenericErrorCode,

                    Message =
                        BuildExceptionMessage(
                            "ManagementException",
                            ex)
                };
            }
            catch (Exception ex)
            {
                return new SystemRestorePointResult
                {
                    IsSuccessful =
                        false,

                    ReturnCode =
                        GenericErrorCode,

                    Message =
                        BuildExceptionMessage(
                            $"Exception: {ex.GetType().FullName}",
                            ex)
                };
            }
        }


        // ======================================
        // RESTORE SYSTEM - WMI
        // ======================================

        private static SystemRestorePointResult
            RestoreSystem(
                uint sequenceNumber)
        {
            try
            {
                if (sequenceNumber == 0)
                {
                    return new SystemRestorePointResult
                    {
                        IsSuccessful =
                            false,

                        ReturnCode =
                            GenericErrorCode,

                        Message =
                            "The selected restore point is invalid."
                    };
                }

                ManagementScope scope =
                    CreateManagementScope();

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
                    return new SystemRestorePointResult
                    {
                        IsSuccessful =
                            false,

                        ReturnCode =
                            GenericErrorCode,

                        Message =
                            "System Restore parameters could not be created."
                    };
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
                        ?? GenericErrorCode);

                if (returnCode ==
                    SuccessCode)
                {
                    return new SystemRestorePointResult
                    {
                        IsSuccessful =
                            true,

                        ReturnCode =
                            SuccessCode,

                        Message =
                            "System Restore was started successfully. " +
                            "Windows must be restarted to complete the restoration."
                    };
                }

                return new SystemRestorePointResult
                {
                    IsSuccessful =
                        false,

                    ReturnCode =
                        returnCode,

                    Message =
                        $"System Restore could not be started. " +
                        $"Code: {returnCode}"
                };
            }
            catch (UnauthorizedAccessException ex)
            {
                return new SystemRestorePointResult
                {
                    IsSuccessful =
                        false,

                    ReturnCode =
                        AccessDeniedCode,

                    Message =
                        BuildExceptionMessage(
                            "Access denied",
                            ex)
                };
            }
            catch (ManagementException ex)
            {
                return new SystemRestorePointResult
                {
                    IsSuccessful =
                        false,

                    ReturnCode =
                        GenericErrorCode,

                    Message =
                        BuildExceptionMessage(
                            "System Restore error",
                            ex)
                };
            }
            catch (Exception ex)
            {
                return new SystemRestorePointResult
                {
                    IsSuccessful =
                        false,

                    ReturnCode =
                        GenericErrorCode,

                    Message =
                        BuildExceptionMessage(
                            "System Restore error",
                            ex)
                };
            }
        }


        // ======================================
        // ENABLE SYSTEM PROTECTION
        // ======================================

        private static SystemProtectionResult
            EnableSystemProtection()
        {
            try
            {
                string systemDrive =
                    Environment.GetEnvironmentVariable(
                        "SystemDrive")
                    ?? "C:";

                if (!systemDrive.EndsWith(
                        "\\",
                        StringComparison.Ordinal))
                {
                    systemDrive += "\\";
                }

                ManagementScope scope =
                    CreateManagementScope();

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
                                "Enable");

                if (inputParameters == null)
                {
                    return new SystemProtectionResult
                    {
                        IsSuccessful =
                            false,

                        Message =
                            "System Protection parameters could not be created."
                    };
                }

                inputParameters[
                    "Drive"] =
                        systemDrive;

                using ManagementBaseObject?
                    outputParameters =
                        restoreClass
                            .InvokeMethod(
                                "Enable",
                                inputParameters,
                                null);

                uint returnCode =
                    Convert.ToUInt32(
                        outputParameters?[
                            "ReturnValue"]
                        ?? GenericErrorCode);

                if (returnCode ==
                    SuccessCode)
                {
                    return new SystemProtectionResult
                    {
                        IsSuccessful =
                            true,

                        Message =
                            "System Protection was enabled successfully."
                    };
                }

                return new SystemProtectionResult
                {
                    IsSuccessful =
                        false,

                    Message =
                        $"System Protection could not be enabled. " +
                        $"Code: {returnCode}"
                };
            }
            catch (UnauthorizedAccessException ex)
            {
                return new SystemProtectionResult
                {
                    IsSuccessful =
                        false,

                    Message =
                        BuildExceptionMessage(
                            "Access denied",
                            ex)
                };
            }
            catch (ManagementException ex)
            {
                return new SystemProtectionResult
                {
                    IsSuccessful =
                        false,

                    Message =
                        BuildExceptionMessage(
                            "System Protection error",
                            ex)
                };
            }
            catch (Exception ex)
            {
                return new SystemProtectionResult
                {
                    IsSuccessful =
                        false,

                    Message =
                        BuildExceptionMessage(
                            "System Protection error",
                            ex)
                };
            }
        }


        // ======================================
        // EXCEPTION MESSAGE
        // ======================================

        private static string
            BuildExceptionMessage(
                string prefix,
                Exception exception)
        {
            string exceptionMessage =
                string.IsNullOrWhiteSpace(
                    exception.Message)
                    ? "(empty)"
                    : exception.Message;

            return
                $"{prefix} | " +
                $"HRESULT: 0x{exception.HResult:X8} | " +
                $"Message: {exceptionMessage}";
        }
    }
}