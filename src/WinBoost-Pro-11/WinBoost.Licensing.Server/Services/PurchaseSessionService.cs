using System;
using System.Linq;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using WinBoost.Licensing.Server.Data;
using WinBoost.Licensing.Server.Models;

namespace WinBoost.Licensing.Server.Services
{
    public sealed class PurchaseSessionService
    {
        private const string ProductName =
            "WinBoost Pro 11";

        private readonly LicenseOffersService
            _licenseOffersService;

        private readonly IPaymentProvider
            _paymentProvider;

        private readonly PurchaseRepository
            _purchaseRepository;

        private readonly LicenseRepository
            _licenseRepository;

        public PurchaseSessionService(
            LicenseOffersService licenseOffersService,
            IPaymentProvider paymentProvider,
            PurchaseRepository purchaseRepository,
            LicenseRepository licenseRepository)
        {
            _licenseOffersService =
                licenseOffersService;

            _paymentProvider =
                paymentProvider;

            _purchaseRepository =
                purchaseRepository;

            _licenseRepository =
                licenseRepository;
        }

        public async Task<PurchaseSessionResponse>
            CreatePurchaseSessionAsync(
                PurchaseSessionRequest request,
                CancellationToken cancellationToken =
                    default)
        {
            if (request == null)
            {
                return Error(
                    "INVALID_REQUEST",
                    "Purchase request is invalid.");
            }

            string email =
                request.CustomerEmail?.Trim()
                ?? string.Empty;

            string deviceId =
                request.DeviceId?.Trim()
                ?? string.Empty;

            if (!IsValidEmail(
                    email))
            {
                return Error(
                    "INVALID_EMAIL",
                    "Customer e-mail address is invalid.");
            }

            if (string.IsNullOrWhiteSpace(
                    deviceId))
            {
                return Error(
                    "INVALID_DEVICE",
                    "Device identifier is missing.");
            }

            if (!string.Equals(
                    request.ProductName,
                    ProductName,
                    StringComparison.Ordinal))
            {
                return Error(
                    "INVALID_PRODUCT",
                    "Product is invalid.");
            }

            if (request.Plan ==
                LicensePlan.Unknown)
            {
                return Error(
                    "INVALID_PLAN",
                    "License plan is invalid.");
            }

            // ======================================
            // EXISTING ACTIVE LICENSE CHECK
            // ======================================

            LicenseRecord? activeLicense =
                await _licenseRepository
                    .FindActiveByDeviceAsync(
                        deviceId,
                        ProductName,
                        DateTime.UtcNow,
                        cancellationToken);

            if (activeLicense != null)
            {
                return Error(
                    "ACTIVE_LICENSE_EXISTS",
                    "An active WinBoost Pro 11 license already exists for this device.");
            }

            // ======================================
            // CHECK CURRENT AVAILABLE OFFERS
            // ======================================

            LicenseOffersResponse offersResponse =
                _licenseOffersService
                    .GetCurrentOffers();

            bool planAvailable =
                offersResponse.Offers.Any(
                    offer =>
                        offer.IsAvailable &&
                        offer.Plan ==
                            request.Plan);

            if (!planAvailable)
            {
                return Error(
                    "PLAN_NOT_AVAILABLE",
                    "The selected license plan is not available.");
            }

            // ======================================
            // CREATE PADDLE CHECKOUT
            // ======================================

            PaymentCheckoutResult checkout =
                await _paymentProvider
                    .CreateCheckoutAsync(
                        request,
                        cancellationToken);

            if (!checkout.Success)
            {
                return Error(
                    checkout.ErrorCode,
                    checkout.Message);
            }

            if (string.IsNullOrWhiteSpace(
                    checkout.ProviderSessionId))
            {
                return Error(
                    "INVALID_PROVIDER_RESPONSE",
                    "The payment provider did not return a session identifier.");
            }

            if (!IsValidCheckoutUrl(
                    checkout.CheckoutUrl))
            {
                return Error(
                    "INVALID_CHECKOUT_URL",
                    "The payment provider returned an invalid checkout URL.");
            }

            // ======================================
            // SAVE PENDING PURCHASE
            // ======================================

            try
            {
                await _purchaseRepository
                    .CreatePendingAsync(
                        checkout.ProviderSessionId,
                        email,
                        deviceId,
                        ProductName,
                        request.Plan,
                        _paymentProvider
                            .GetType()
                            .Name,
                        cancellationToken);
            }
            catch
            {
                return Error(
                    "DATABASE_ERROR",
                    "The purchase session could not be saved.");
            }

            return new PurchaseSessionResponse
            {
                Success =
                    true,

                CheckoutUrl =
                    checkout.CheckoutUrl,

                SessionId =
                    checkout.ProviderSessionId,

                ErrorCode =
                    string.Empty,

                Message =
                    string.Empty
            };
        }

        private static bool IsValidEmail(
            string email)
        {
            return MailAddress.TryCreate(
                email,
                out MailAddress? address) &&
                string.Equals(
                    address.Address,
                    email,
                    StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsValidCheckoutUrl(
            string checkoutUrl)
        {
            return Uri.TryCreate(
                checkoutUrl,
                UriKind.Absolute,
                out Uri? uri) &&
                uri.Scheme ==
                    Uri.UriSchemeHttps;
        }

        private static PurchaseSessionResponse
            Error(
                string errorCode,
                string message)
        {
            return new PurchaseSessionResponse
            {
                Success =
                    false,

                CheckoutUrl =
                    string.Empty,

                SessionId =
                    string.Empty,

                ErrorCode =
                    errorCode ?? string.Empty,

                Message =
                    message ?? string.Empty
            };
        }
    }
}