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
    }
}