using System;

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
    }
}