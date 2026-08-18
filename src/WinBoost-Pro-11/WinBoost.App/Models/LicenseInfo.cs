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
                           ExpiresAt.Value > DateTime.Now;
                }

                if (Status == LicenseStatus.Trial)
                {
                    return ExpiresAt.HasValue &&
                           ExpiresAt.Value > DateTime.Now;
                }

                return false;
            }
        }

        public int? RemainingDays
        {
            get
            {
                if (!ExpiresAt.HasValue)
                {
                    return null;
                }

                TimeSpan remaining =
                    ExpiresAt.Value.Date -
                    DateTime.Now.Date;

                return Math.Max(
                    0,
                    remaining.Days);
            }
        }
    }
}