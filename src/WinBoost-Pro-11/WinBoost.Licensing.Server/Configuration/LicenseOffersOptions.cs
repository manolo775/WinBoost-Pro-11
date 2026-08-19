using System;

namespace WinBoost.Licensing.Server.Configuration
{
    public sealed class LicenseOffersOptions
    {
        public const string SectionName =
            "LicensingOffers";

        public string Currency
        {
            get;
            set;
        } = "EUR";

        public DateTimeOffset?
            PromotionStartsAtUtc
        {
            get;
            set;
        }

        public DateTimeOffset?
            PromotionEndsAtUtc
        {
            get;
            set;
        }

        public decimal?
            PromotionalLifetimePrice
        {
            get;
            set;
        }

        public decimal?
            OneMonthPrice
        {
            get;
            set;
        }

        public decimal?
            ThreeMonthsPrice
        {
            get;
            set;
        }

        public decimal?
            SixMonthsPrice
        {
            get;
            set;
        }

        public decimal?
            OneYearPrice
        {
            get;
            set;
        }
    }
}