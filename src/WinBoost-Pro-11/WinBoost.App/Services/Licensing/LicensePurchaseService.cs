using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using WinBoost.App.Models;

namespace WinBoost.App.Services.Licensing
{
    public sealed class LicensePurchaseService
    {
        private readonly DeviceIdentityService
            _deviceIdentityService;

        private readonly PurchaseApiClient
            _apiClient;

        public LicensePurchaseService()
        {
            _deviceIdentityService =
                new DeviceIdentityService();

            _apiClient =
                new PurchaseApiClient(
                    LicenseSecurityConfiguration
                        .PurchaseSessionEndpoint);
        }

        public async Task<PurchaseSessionResponse>
            StartPurchaseAsync(
                string customerEmail,
                LicensePlan plan,
                CancellationToken cancellationToken =
                    default)
        {
            string normalizedEmail =
                customerEmail
                    .Trim();

            if (string.IsNullOrWhiteSpace(
                    normalizedEmail))
            {
                return new PurchaseSessionResponse
                {
                    Success = false,

                    ErrorCode =
                        "INVALID_EMAIL",

                    Message =
                        "The customer email address is required."
                };
            }

            if (plan == LicensePlan.Unknown)
            {
                return new PurchaseSessionResponse
                {
                    Success = false,

                    ErrorCode =
                        "INVALID_PLAN",

                    Message =
                        "A valid license plan is required."
                };
            }

            var request =
                new PurchaseSessionRequest
                {
                    CustomerEmail =
                        normalizedEmail,

                    DeviceId =
                        _deviceIdentityService
                            .GetDeviceId(),

                    ProductName =
                        LicenseSecurityConfiguration
                            .ProductName,

                    Plan =
                        plan
                };

            PurchaseSessionResponse response =
                await _apiClient
                    .CreateSessionAsync(
                        request,
                        cancellationToken);

            if (!response.Success)
            {
                return response;
            }

            if (!TryOpenCheckoutPage(
                    response.CheckoutUrl))
            {
                return new PurchaseSessionResponse
                {
                    Success = false,

                    CheckoutUrl =
                        response.CheckoutUrl,

                    SessionId =
                        response.SessionId,

                    ErrorCode =
                        "INVALID_CHECKOUT_URL",

                    Message =
                        "The checkout URL returned by the server is invalid."
                };
            }

            return response;
        }

        private static bool TryOpenCheckoutPage(
            string checkoutUrl)
        {
            if (!Uri.TryCreate(
                    checkoutUrl,
                    UriKind.Absolute,
                    out Uri? checkoutUri) ||
                checkoutUri.Scheme !=
                    Uri.UriSchemeHttps)
            {
                return false;
            }

            try
            {
                Process.Start(
                    new ProcessStartInfo
                    {
                        FileName =
                            checkoutUri.AbsoluteUri,

                        UseShellExecute =
                            true
                    });

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}