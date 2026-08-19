using System;

namespace WinBoost.App.Models
{
    public sealed class SignedLicenseResponse
    {
        public string LicenseId
        {
            get;
            init;
        } = string.Empty;

        public string CustomerEmail
        {
            get;
            init;
        } = string.Empty;

        public string ProductName
        {
            get;
            init;
        } = string.Empty;

        public string LicenseType
        {
            get;
            init;
        } = string.Empty;

        public LicensePlan Plan
        {
            get;
            init;
        } = LicensePlan.Unknown;
        public DateTime ActivatedAt
        {
            get;
            init;
        }

        public DateTime? ExpiresAt
        {
            get;
            init;
        }

        public string DeviceId
        {
            get;
            init;
        } = string.Empty;

        public string Signature
        {
            get;
            init;
        } = string.Empty;
    }
}