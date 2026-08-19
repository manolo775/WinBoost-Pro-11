using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using WinBoost.Licensing.Server.Configuration;
using WinBoost.Licensing.Server.Models;

namespace WinBoost.Licensing.Server.Services
{
    public sealed class LicenseOffersService
    {
        private readonly LicenseOffersOptions
            _options;

        public LicenseOffersService(
            IOptions<LicenseOffersOptions> options)
        {
            _options =
                options.Value;
        }

        public LicenseOffersResponse
            GetCurrentOffers()
        {
            DateTimeOffset nowUtc =
                DateTimeOffset.UtcNow;

            bool promotionConfigured =
                _options.PromotionStartsAtUtc.HasValue &&
                _options.PromotionEndsAtUtc.HasValue;

            bool promotionActive =
                promotionConfigured &&
                nowUtc >=
                    _options.PromotionStartsAtUtc!.Value &&
                nowUtc <=
                    _options.PromotionEndsAtUtc!.Value;

            var offers =
                new List<LicenseOffer>();

            if (promotionActive)
            {
                if (_options
                    .PromotionalLifetimePrice
                    .HasValue)
                {
                    offers.Add(
                        new LicenseOffer
                        {
                            Plan =
                                LicensePlan
                                    .PromotionalLifetime,

                            Price =
                                _options
                                    .PromotionalLifetimePrice
                                    .Value,

                            Currency =
                                _options.Currency,

                            IsAvailable =
                                true,

                            IsPromotional =
                                true,

                            PromotionEndsAt =
                                _options
                                    .PromotionEndsAtUtc
                                    ?.UtcDateTime
                        });
                }
            }
            else
            {
                AddOfferIfConfigured(
                    offers,
                    LicensePlan.OneMonth,
                    _options.OneMonthPrice);

                AddOfferIfConfigured(
                    offers,
                    LicensePlan.ThreeMonths,
                    _options.ThreeMonthsPrice);

                AddOfferIfConfigured(
                    offers,
                    LicensePlan.SixMonths,
                    _options.SixMonthsPrice);

                AddOfferIfConfigured(
                    offers,
                    LicensePlan.OneYear,
                    _options.OneYearPrice);
            }

            return new LicenseOffersResponse
            {
                Success =
                    true,

                Offers =
                    offers,

                ErrorCode =
                    string.Empty,

                Message =
                    string.Empty
            };
        }

        private void AddOfferIfConfigured(
            List<LicenseOffer> offers,
            LicensePlan plan,
            decimal? price)
        {
            if (!price.HasValue)
            {
                return;
            }

            offers.Add(
                new LicenseOffer
                {
                    Plan =
                        plan,

                    Price =
                        price.Value,

                    Currency =
                        _options.Currency,

                    IsAvailable =
                        true,

                    IsPromotional =
                        false,

                    PromotionEndsAt =
                        null
                });
        }
    }
}