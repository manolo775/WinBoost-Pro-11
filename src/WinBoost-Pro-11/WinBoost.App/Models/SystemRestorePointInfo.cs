using System;
using WinBoost.App.Helpers;
using WinBoost.App.Localization;

namespace WinBoost.App.Models
{
    public sealed class SystemRestorePointInfo
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

        public string RestorePointTypeName
        {
            get;
            init;
        } = string.Empty;

        public DateTime CreatedAt
        {
            get;
            init;
        }

        public string CreatedAtDisplay =>
            CreatedAt == DateTime.MinValue
                ? "-"
                : CreatedAt.ToString(
                    "dd.MM.yyyy HH:mm:ss");

        public bool IsWinBoostRestorePoint =>
            Description.StartsWith(
                "WinBoost Pro 11",
                StringComparison.OrdinalIgnoreCase);


        // ======================================
        // LOCALIZED DESCRIPTION
        // ======================================

        public string DisplayDescription
        {
            get
            {
                if (Description.Equals(
                        "Restore Operation",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return LocalizationHelper.Get(
                        "RecoveryRestoreOperation");
                }

                if (Description.StartsWith(
                        "WinBoost Pro 11 - Safety Restore Point",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return LocalizationHelper.Get(
                        "RecoveryWinBoostSafetyRestorePoint");
                }

                return Description;
            }
        }


        // ======================================
        // LOCALIZED RESTORE POINT TYPE
        // ======================================

        public string DisplayRestorePointTypeName
        {
            get
            {
                return RestorePointType switch
                {
                    0 =>
                        LocalizationHelper.Get(
                            "RecoveryTypeApplicationInstall"),

                    1 =>
                        LocalizationHelper.Get(
                            "RecoveryTypeApplicationUninstall"),

                    10 =>
                        LocalizationHelper.Get(
                            "RecoveryTypeDeviceDriverInstall"),

                    12 =>
                        LocalizationHelper.Get(
                            "RecoveryTypeSystem"),

                    13 =>
                        LocalizationHelper.Get(
                            "RecoveryTypeCancelledOperation"),

                    _ =>
                        LocalizationHelper.Get(
                            "RecoveryTypeOther")
                };
            }
        }
    }
}