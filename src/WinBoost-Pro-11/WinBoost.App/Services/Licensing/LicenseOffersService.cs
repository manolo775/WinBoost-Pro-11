using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WinBoost.App.Helpers;
using WinBoost.App.Models;

namespace WinBoost.App.Services.Licensing
{
    public sealed class LicenseOffersService
    {
        private readonly LicenseOffersApiClient
            _apiClient;

        public LicenseOffersService()
        {
            _apiClient =
                new LicenseOffersApiClient(
                    LicenseSecurityConfiguration
                        .LicenseOffersEndpoint);
        }

        public async Task<LicenseOffersResponse>
            GetAvailableOffersAsync(
                CancellationToken cancellationToken =
                    default)
        {
            LicenseOffersResponse response =
                await _apiClient
                    .GetOffersAsync(
                        cancellationToken);

            if (!response.Success)
            {
                return response;
            }

            List<LicenseOffer> availableOffers =
                response.Offers
                    .Where(
                        offer =>
                            offer.IsAvailable &&
                            offer.Plan !=
                                LicensePlan.Unknown)
                    .ToList();

            return new LicenseOffersResponse
            {
                Success =
                    true,

                Offers =
                    availableOffers,

                ErrorCode =
                    response.ErrorCode,

                Message =
                    response.Message
            };
        }

        public async Task<
            IReadOnlyList<LicenseOfferDisplayItem>>
            GetDisplayOffersAsync(
                CancellationToken cancellationToken =
                    default)
        {
            LicenseOffersResponse response =
                await GetAvailableOffersAsync(
                    cancellationToken);

            if (!response.Success)
            {
                return
                    new List<
                        LicenseOfferDisplayItem>();
            }

            return response.Offers
                .Select(
                    CreateDisplayItem)
                .ToList();
        }

        private static LicenseOfferDisplayItem
            CreateDisplayItem(
                LicenseOffer offer)
        {
            string displayName =
                LicensePlanDisplayHelper
                    .GetDisplayName(
                        offer.Plan);

            string currency =
                offer.Currency
                    .Trim()
                    .ToUpperInvariant();

            string priceText =
                string.Format(
                    CultureInfo.CurrentCulture,
                    "{0:N2} {1}",
                    offer.Price,
                    currency);

            return new LicenseOfferDisplayItem
            {
                Plan =
                    offer.Plan,

                DisplayName =
                    displayName,

                Price =
                    offer.Price,

                Currency =
                    currency,

                IsPromotional =
                    offer.IsPromotional,

                PromotionEndsAt =
                    offer.PromotionEndsAt,

                PriceText =
                    priceText
            };
        }
    }
}