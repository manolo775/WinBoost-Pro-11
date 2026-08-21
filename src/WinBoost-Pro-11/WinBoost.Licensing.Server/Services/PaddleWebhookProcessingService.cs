using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using WinBoost.Licensing.Server.Configuration;
using WinBoost.Licensing.Server.Data;
using WinBoost.Licensing.Server.Models;

namespace WinBoost.Licensing.Server.Services
{
    public sealed class PaddleWebhookProcessingResult
    {
        public bool Success
        {
            get;
            init;
        }

        public bool PaymentCompleted
        {
            get;
            init;
        }

        public string ErrorCode
        {
            get;
            init;
        } = string.Empty;

        public string Message
        {
            get;
            init;
        } = string.Empty;
    }

    public sealed class PaddleWebhookProcessingService
    {
        private readonly PurchaseRepository
            _purchaseRepository;

        private readonly IPaymentProvider
            _paymentProvider;

        private readonly PaddleOptions
            _paddleOptions;

        private readonly LicenseIssuerService
            _licenseIssuerService;

        public PaddleWebhookProcessingService(
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

        public async Task<PaddleWebhookProcessingResult>
            ProcessTransactionCompletedAsync(
                string transactionId,
                CancellationToken cancellationToken =
                    default)
        {
            transactionId =
                transactionId?.Trim()
                ?? string.Empty;

            if (string.IsNullOrWhiteSpace(
                    transactionId))
            {
                return Error(
                    "INVALID_TRANSACTION_ID",
                    "The Paddle transaction ID is missing.");
            }

            PurchaseRecord? purchase =
                await _purchaseRepository
                    .FindByTransactionIdAsync(
                        transactionId,
                        cancellationToken);

            if (purchase == null)
            {
                return Error(
                    "PURCHASE_NOT_FOUND",
                    "The purchase associated with the Paddle transaction was not found.");
            }

            PaymentTransactionStatusResult transaction =
                await _paymentProvider
                    .GetTransactionStatusAsync(
                        transactionId,
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
                    "The Paddle transaction does not match the stored purchase.");
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
                    "The Paddle transaction data does not match the stored purchase.");
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
                return Error(
                    "PAYMENT_NOT_COMPLETED",
                    "Paddle reports that the payment has not been completed.");
            }

            try
            {
                await _purchaseRepository
                    .MarkCompletedAsync(
                        purchase,
                        transactionId,
                        cancellationToken);

                await _licenseIssuerService
                    .IssueAsync(
                        purchase,
                        cancellationToken);

                return new PaddleWebhookProcessingResult
                {
                    Success =
                        true,

                    PaymentCompleted =
                        true,

                    ErrorCode =
                        string.Empty,

                    Message =
                        "The Paddle transaction was processed and the license was issued successfully."
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
                    "WEBHOOK_PROCESSING_ERROR",
                    "The Paddle transaction could not be processed.");
            }
        }

        private string GetExpectedPriceId(
            LicensePlan plan)
        {
            return plan switch
            {
                LicensePlan.PromotionalLifetime =>
                    _paddleOptions
                        .PromotionalLifetimePriceId,

                LicensePlan.OneMonth =>
                    _paddleOptions
                        .OneMonthPriceId,

                LicensePlan.ThreeMonths =>
                    _paddleOptions
                        .ThreeMonthsPriceId,

                LicensePlan.SixMonths =>
                    _paddleOptions
                        .SixMonthsPriceId,

                LicensePlan.OneYear =>
                    _paddleOptions
                        .OneYearPriceId,

                _ =>
                    string.Empty
            };
        }

        private static PaddleWebhookProcessingResult
            Error(
                string errorCode,
                string message)
        {
            return new PaddleWebhookProcessingResult
            {
                Success =
                    false,

                PaymentCompleted =
                    false,

                ErrorCode =
                    errorCode ?? string.Empty,

                Message =
                    message ?? string.Empty
            };
        }
    }
}