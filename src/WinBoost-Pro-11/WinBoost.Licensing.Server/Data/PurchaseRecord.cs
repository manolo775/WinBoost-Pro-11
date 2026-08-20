using System;
using WinBoost.Licensing.Server.Models;

namespace WinBoost.Licensing.Server.Data
{
    public sealed class PurchaseRecord
    {
        public long Id
        {
            get;
            set;
        }

        public string SessionId
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

        public LicensePlan Plan
        {
            get;
            set;
        } = LicensePlan.Unknown;

        public string Status
        {
            get;
            set;
        } = "Pending";

        public string PaymentProvider
        {
            get;
            set;
        } = string.Empty;

        public string ProviderTransactionId
        {
            get;
            set;
        } = string.Empty;

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