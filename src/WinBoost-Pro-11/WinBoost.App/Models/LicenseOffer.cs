using System;

namespace WinBoost.App.Models
{
    public sealed class LicenseOffer
    {
        public LicensePlan Plan
        {
            get;
            init;
        } = LicensePlan.Unknown;

        public decimal Price
        {
            get;
            init;
        }

        public string Currency
        {
            get;
            init;
        } = string.Empty;

        public bool IsPromotional
        {
            get;
            init;
        }

        public DateTime? PromotionEndsAt
        {
            get;
            init;
        }

        public bool IsAvailable
        {
            get;
            init;
        }
    }
}