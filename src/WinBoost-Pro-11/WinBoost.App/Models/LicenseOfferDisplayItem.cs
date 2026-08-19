using System;

namespace WinBoost.App.Models
{
    public sealed class LicenseOfferDisplayItem
    {
        public LicensePlan Plan
        {
            get;
            init;
        } = LicensePlan.Unknown;

        public string DisplayName
        {
            get;
            init;
        } = string.Empty;

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

        public string PriceText
        {
            get;
            init;
        } = string.Empty;
    }
}