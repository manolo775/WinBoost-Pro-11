using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using WinBoost.Licensing.Server.Configuration;
using WinBoost.Licensing.Server.Data;
using WinBoost.Licensing.Server.Models;

namespace WinBoost.Licensing.Server.Services
{
    public sealed class LicenseActivationCheckService
    {
        private const string ProductName =
            "WinBoost Pro 11";

        private readonly PurchaseRepository
            _purchaseRepository;

        private readonly IPaymentProvider
            _paymentProvider;

        private readonly PaddleOptions
            _paddleOptions;

        private readonly LicenseIssuerService
            _licenseIssuerService;

        public LicenseActivationCheckService(
            PurchaseRepository purchaseRepository,
            IPaymentProvider paymentProvider,
            IOptions<PaddleOptions> paddleOptions,
            LicenseIssuerService licenseIssuerService)
        {
            _purchaseRepository =
                purchaseRepository;

            _paymentProvider =
                paymentProvider;

            _paddleOptions =
                paddleOptions.Value;

            _licenseIssuerService =
                licenseIssuerService;
        }

        public async Task<LicenseActivationCheckResponse>
            VerifyPaymentAsync(
                LicenseActivationCheckRequest request,
                CancellationToken cancellationToken =
                    default)
        {
            if (request == null)
            {
                return Error(
                    "INVALID_REQUEST",
                    "Activation request is invalid.");
            }

            string email =
                request.CustomerEmail?.Trim()
                ?? string.Empty;

            string sessionId =
                request.PurchaseSessionId?.Trim()
                ?? string.Empty;

            string deviceId =
                request.DeviceId?.Trim()
                ?? string.Empty;

            if (string.IsNullOrWhiteSpace(
                    email) ||
                string.IsNullOrWhiteSpace(
                    sessionId) ||
                string.IsNullOrWhiteSpace(
                    deviceId))
            {
                return Error(
                    "INVALID_REQUEST",
                    "Activation request is incomplete.");
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

            PurchaseRecord? purchase =
                await _purchaseRepository
                    .FindBySessionIdAsync(
                        sessionId,
                        cancellationToken);

            if (purchase == null)
            {
                return Error(
                    "PURCHASE_NOT_FOUND",
                    "The purchase session was not found.");
            }

            if (!string.Equals(
                    purchase.CustomerEmail,
                    email,
                    StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(
                    purchase.DeviceId,
                    deviceId,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    purchase.ProductName,
                    ProductName,
                    StringComparison.Ordinal))
            {
                return Error(
                    "PURCHASE_MISMATCH",
                    "The purchase does not match the activation request.");
            }

            PaymentTransactionStatusResult transaction =
                await _paymentProvider
                    .GetTransactionStatusAsync(
                        sessionId,
                        cancellationToken);

            if (!transaction.Success)
            {
                return Error(
                    transaction.ErrorCode,
                    transaction.Message);
            }

            if (!string.Equals(
                    transaction.ProviderSessionId,
                    purchase.SessionId,
                    StringComparison.Ordinal))
            {
                return Error(
                    "TRANSACTION_MISMATCH",
                    "The payment transaction does not match the purchase.");
            }

            if (!string.Equals(
                    transaction.CustomerEmail,
                    purchase.CustomerEmail,
                    StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(
                    transaction.DeviceId,
                    purchase.DeviceId,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    transaction.ProductName,
                    purchase.ProductName,
                    StringComparison.Ordinal) ||
                transaction.Plan !=
                    purchase.Plan)
            {
                return Error(
                    "TRANSACTION_DATA_MISMATCH",
                    "The payment transaction data does not match the purchase.");
            }

            string expectedPriceId =
                GetExpectedPriceId(
                    purchase.Plan);

            if (string.IsNullOrWhiteSpace(
                    expectedPriceId))
            {
                return Error(
                    "PRICE_NOT_CONFIGURED",
                    "The expected Paddle price is not configured.");
            }

            if (!string.Equals(
                    transaction.PriceId,
                    expectedPriceId,
                    StringComparison.Ordinal))
            {
                return Error(
                    "PRICE_MISMATCH",
                    "The Paddle price does not match the purchased license plan.");
            }

            if (!transaction.PaymentCompleted)
            {
                return new LicenseActivationCheckResponse
                {
                    Success =
                        true,

                    PaymentCompleted =
                        false,

                    ErrorCode =
                        string.Empty,

                    Message =
                        "Payment has not been completed yet.",

                    License =
                        null
                };
            }

            try
            {
                SignedLicenseResponse signedLicense =
                    await _licenseIssuerService
                        .IssueAsync(
                            purchase,
                            cancellationToken);

                return new LicenseActivationCheckResponse
                {
                    Success =
                        true,

                    PaymentCompleted =
                        true,

                    ErrorCode =
                        string.Empty,

                    Message =
                        "The license was issued successfully.",

                    License =
                        signedLicense
                };
            }
            catch (InvalidOperationException exception)
            {
                return Error(
                    "LICENSE_ISSUING_ERROR",
                    exception.Message);
            }
            catch
            {
                return Error(
                    "LICENSE_ISSUING_ERROR",
                    "The license could not be issued.");
            }
        }

        private string GetExpectedPriceId(
    LicensePlan plan)
        {
            return plan switch
            {
                LicensePlan.PromotionalLifetime =>
                    _paddleOptions.PromotionalLifetimePriceId,

                LicensePlan.OneMonth =>
                    _paddleOptions.OneMonthPriceId,

                LicensePlan.ThreeMonths =>
                    _paddleOptions.ThreeMonthsPriceId,

                LicensePlan.SixMonths =>
                    _paddleOptions.SixMonthsPriceId,

                LicensePlan.OneYear =>
                    _paddleOptions.OneYearPriceId,

                _ =>
                    string.Empty
            };
        }

        private static LicenseActivationCheckResponse
            Error(
                string errorCode,
                string message)
        {
            return new LicenseActivationCheckResponse
            {
                Success =
                    false,

                PaymentCompleted =
                    false,

                ErrorCode =
                    errorCode ?? string.Empty,

                Message =
                    message ?? string.Empty,

                License =
                    null
            };
        }
    }
}