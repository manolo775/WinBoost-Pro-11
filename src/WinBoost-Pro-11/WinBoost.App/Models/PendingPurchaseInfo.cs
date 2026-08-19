using System;

namespace WinBoost.App.Models
{
    public sealed class PendingPurchaseInfo
    {
        public string CustomerEmail
        {
            get;
            init;
        } = string.Empty;

        public string PurchaseSessionId
        {
            get;
            init;
        } = string.Empty;

        public LicensePlan Plan
        {
            get;
            init;
        } = LicensePlan.Unknown;

        public DateTime CreatedAt
        {
            get;
            init;
        } = DateTime.UtcNow;
    }
}