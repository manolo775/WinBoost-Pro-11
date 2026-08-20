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

        private readonly PaddleOptions
            _paddleOptions;

        public LicenseOffersService(
            IOptions<LicenseOffersOptions> options,
            IOptions<PaddleOptions> paddleOptions)
        {
            _options =
                options.Value;

            _paddleOptions =
                paddleOptions.Value;
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
                AddOfferIfConfigured(
                    offers,
                    LicensePlan.PromotionalLifetime,
                    _options.PromotionalLifetimePrice,
                    _paddleOptions
                        .PromotionalLifetimePriceId,
                    true,
                    _options
                        .PromotionEndsAtUtc
                        ?.UtcDateTime);
            }
            else
            {
                AddOfferIfConfigured(
                    offers,
                    LicensePlan.OneMonth,
                    _options.OneMonthPrice,
                    _paddleOptions.OneMonthPriceId);

                AddOfferIfConfigured(
                    offers,
                    LicensePlan.ThreeMonths,
                    _options.ThreeMonthsPrice,
                    _paddleOptions.ThreeMonthsPriceId);

                AddOfferIfConfigured(
                    offers,
                    LicensePlan.SixMonths,
                    _options.SixMonthsPrice,
                    _paddleOptions.SixMonthsPriceId);

                AddOfferIfConfigured(
                    offers,
                    LicensePlan.OneYear,
                    _options.OneYearPrice,
                    _paddleOptions.OneYearPriceId);
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
            decimal? price,
            string paddlePriceId,
            bool isPromotional = false,
            DateTime? promotionEndsAt = null)
        {
            if (!price.HasValue ||
                price.Value < 0 ||
                string.IsNullOrWhiteSpace(
                    paddlePriceId))
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
                        isPromotional,

                    PromotionEndsAt =
                        promotionEndsAt
                });
        }
    }
}