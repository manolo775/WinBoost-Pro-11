using System;
using WinBoost.Licensing.Server.Models;

namespace WinBoost.Licensing.Server.Data
{
    public sealed class LicenseRecord
    {
        public long Id
        {
            get;
            set;
        }

        public string LicenseId
        {
            get;
            set;
        } = string.Empty;

        public string CustomerEmail
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

        public string LicenseType
        {
            get;
            set;
        } = "Licensed";

        public LicensePlan Plan
        {
            get;
            set;
        } = LicensePlan.Unknown;

        public string PurchaseSessionId
        {
            get;
            set;
        } = string.Empty;

        public DateTime ActivatedAtUtc
        {
            get;
            set;
        }

        public DateTime? ExpiresAtUtc
        {
            get;
            set;
        }

        public bool IsRevoked
        {
            get;
            set;
        }

        public DateTime? RevokedAtUtc
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