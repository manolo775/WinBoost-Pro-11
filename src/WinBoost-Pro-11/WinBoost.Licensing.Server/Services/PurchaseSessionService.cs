using System;
using System.Linq;
using System.Net.Mail;
using WinBoost.Licensing.Server.Models;

namespace WinBoost.Licensing.Server.Services
{
    public sealed class PurchaseSessionService
    {
        private const string ProductName =
            "WinBoost Pro 11";

        private readonly LicenseOffersService
            _licenseOffersService;

        public PurchaseSessionService(
            LicenseOffersService licenseOffersService)
        {
            _licenseOffersService =
                licenseOffersService;
        }

        public PurchaseSessionResponse
            CreatePurchaseSession(
                PurchaseSessionRequest request)
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

            if (!IsValidEmail(email))
            {
                return Error(
                    "INVALID_EMAIL",
                    "Customer e-mail address is invalid.");
            }

            if (string.IsNullOrWhiteSpace(
                request.DeviceId))
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

            LicenseOffersResponse
                offersResponse =
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

            return Error(
                "PAYMENT_PROVIDER_NOT_CONFIGURED",
                "The payment provider is not configured yet.");
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
                    errorCode,

                Message =
                    message
            };
        }
    }
}