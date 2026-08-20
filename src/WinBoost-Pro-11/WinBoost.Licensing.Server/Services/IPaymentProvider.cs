using System.Threading;
using System.Threading.Tasks;
using WinBoost.Licensing.Server.Models;

namespace WinBoost.Licensing.Server.Services
{
    public interface IPaymentProvider
    {
        Task<PaymentCheckoutResult>
            CreateCheckoutAsync(
                PurchaseSessionRequest request,
                CancellationToken cancellationToken =
                    default);
    }
}