using System.Threading;
using System.Threading.Tasks;
using WinBoost.Licensing.Server.Models;

namespace WinBoost.Licensing.Server.Services
{
    public sealed class UnconfiguredPaymentProvider
        : IPaymentProvider
    {
        public Task<PaymentCheckoutResult>
            CreateCheckoutAsync(
                PurchaseSessionRequest request,
                CancellationToken cancellationToken =
                    default)
        {
            return Task.FromResult(
                new PaymentCheckoutResult
                {
                    Success =
                        false,

                    CheckoutUrl =
                        string.Empty,

                    ProviderSessionId =
                        string.Empty,

                    ErrorCode =
                        "PAYMENT_PROVIDER_NOT_CONFIGURED",

                    Message =
                        "The payment provider is not configured yet."
                });
        }

        public Task<PaymentTransactionStatusResult>
            GetTransactionStatusAsync(
                string providerSessionId,
                CancellationToken cancellationToken =
                    default)
        {
            return Task.FromResult(
                new PaymentTransactionStatusResult
                {
                    Success =
                        false,

                    ProviderSessionId =
                        providerSessionId,

                    ErrorCode =
                        "PAYMENT_PROVIDER_NOT_CONFIGURED",

                    Message =
                        "The payment provider is not configured yet."
                });
        }
    }
}