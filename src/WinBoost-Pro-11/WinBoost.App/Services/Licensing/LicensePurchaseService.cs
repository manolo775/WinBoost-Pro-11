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

        private readonly PendingPurchaseService
            _pendingPurchaseService;

        public LicensePurchaseService()
        {
            _deviceIdentityService =
                new DeviceIdentityService();

            _apiClient =
                new PurchaseApiClient(
                    LicenseSecurityConfiguration
                        .PurchaseSessionEndpoint);

            _pendingPurchaseService =
                PendingPurchaseService.Instance;
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

            if (string.IsNullOrWhiteSpace(
                    response.SessionId))
            {
                return new PurchaseSessionResponse
                {
                    Success = false,

                    CheckoutUrl =
                        response.CheckoutUrl,

                    ErrorCode =
                        "INVALID_SERVER_RESPONSE",

                    Message =
                        "The licensing server did not return a purchase session identifier."
                };
            }

            if (!TryCreateCheckoutUri(
                    response.CheckoutUrl,
                    out Uri? checkoutUri))
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

            try
            {
                _pendingPurchaseService
                    .SetPendingPurchase(
                        new PendingPurchaseInfo
                        {
                            CustomerEmail =
                                normalizedEmail,

                            PurchaseSessionId =
                                response.SessionId,

                            Plan =
                                plan,

                            CreatedAt =
                                DateTime.UtcNow
                        });
            }
            catch
            {
                return new PurchaseSessionResponse
                {
                    Success = false,

                    CheckoutUrl =
                        response.CheckoutUrl,

                    SessionId =
                        response.SessionId,

                    ErrorCode =
                        "LOCAL_STORAGE_ERROR",

                    Message =
                        "The pending purchase could not be saved locally."
                };
            }

            if (!TryOpenCheckoutPage(
                    checkoutUri!))
            {
                try
                {
                    _pendingPurchaseService
                        .ClearPendingPurchase();
                }
                catch
                {
                }

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
                        "The secure checkout page could not be opened."
                };
            }

            return response;
        }

        private static bool TryCreateCheckoutUri(
            string checkoutUrl,
            out Uri? checkoutUri)
        {
            checkoutUri =
                null;

            if (!Uri.TryCreate(
                    checkoutUrl,
                    UriKind.Absolute,
                    out Uri? uri) ||
                uri.Scheme !=
                    Uri.UriSchemeHttps)
            {
                return false;
            }

            checkoutUri =
                uri;

            return true;
        }

        private static bool TryOpenCheckoutPage(
            Uri checkoutUri)
        {
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