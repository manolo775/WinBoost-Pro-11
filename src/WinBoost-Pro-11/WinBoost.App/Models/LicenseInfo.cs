using System;

namespace WinBoost.App.Models
{
    public sealed class LicenseInfo
    {
        public LicenseStatus Status
        {
            get;
            set;
        } = LicenseStatus.Unlicensed;

        public string LicenseKey
        {
            get;
            set;
        } = string.Empty;

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

        public string LicenseType
        {
            get;
            set;
        } = string.Empty;

        public LicensePlan Plan
        {
            get;
            set;
        } = LicensePlan.Unknown;

        public DateTime? ActivatedAt
        {
            get;
            set;
        }

        public DateTime? ExpiresAt
        {
            get;
            set;
        }

        public string LicensedTo
        {
            get;
            set;
        } = string.Empty;

        public bool IsActive
        {
            get
            {
                if (Status == LicenseStatus.Licensed)
                {
                    return !ExpiresAt.HasValue ||
                           ExpiresAt.Value
                               .ToUniversalTime() >
                           DateTime.UtcNow;
                }

                if (Status == LicenseStatus.Trial)
                {
                    return ExpiresAt.HasValue &&
                           ExpiresAt.Value
                               .ToUniversalTime() >
                           DateTime.UtcNow;
                }

                return false;
            }
        }

        public TimeSpan? RemainingTime
        {
            get
            {
                if (!ExpiresAt.HasValue)
                {
                    return null;
                }

                TimeSpan remaining =
                    ExpiresAt.Value
                        .ToUniversalTime() -
                    DateTime.UtcNow;

                if (remaining <= TimeSpan.Zero)
                {
                    return TimeSpan.Zero;
                }

                return remaining;
            }
        }

        public int? RemainingDays
        {
            get
            {
                TimeSpan? remaining =
                    RemainingTime;

                if (!remaining.HasValue)
                {
                    return null;
                }

                return Math.Max(
                    0,
                    (int)Math.Ceiling(
                        remaining.Value.TotalDays));
            }
        }
    }
}