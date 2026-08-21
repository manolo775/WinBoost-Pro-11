using System;

namespace WinBoost.Licensing.Server.Data
{
    public sealed class TrialRecord
    {
        public long Id
        {
            get;
            set;
        }

        public string TrialDeviceTokenHash
        {
            get;
            set;
        } = string.Empty;

        public string DeviceId
        {
            get;
            set;
        } = string.Empty;

        public string ProductName
        {
            get;
            set;
        } = string.Empty;

        public string LicenseId
        {
            get;
            set;
        } = string.Empty;

        public DateTime StartedAtUtc
        {
            get;
            set;
        }

        public DateTime ExpiresAtUtc
        {
            get;
            set;
        }

        public DateTime CreatedAtUtc
        {
            get;
            set;
        }

        public DateTime UpdatedAtUtc
        {
            get;
            set;
        }
    }
}