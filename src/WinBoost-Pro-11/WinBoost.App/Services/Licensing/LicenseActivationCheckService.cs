using System;
using System.Threading;
using System.Threading.Tasks;
using WinBoost.App.Models;

namespace WinBoost.App.Services.Licensing
{
    public sealed class LicenseActivationCheckService
    {
        private readonly PendingPurchaseService
            _pendingPurchaseService;

        private readonly DeviceIdentityService
            _deviceIdentityService;

        private readonly LicenseActivationCheckApiClient
            _apiClient;

        private readonly LicenseResponseValidator
            _licenseResponseValidator;

        private readonly LicenseService
            _licenseService;

        private readonly SignedLicenseStorageService
            _signedLicenseStorageService;

        public LicenseActivationCheckService()
        {
            _pendingPurchaseService =
                PendingPurchaseService.Instance;

            _deviceIdentityService =
                new DeviceIdentityService();

            _apiClient =
                new LicenseActivationCheckApiClient(
                    LicenseSecurityConfiguration
                        .ActivationCheckEndpoint);

            _licenseResponseValidator =
                new LicenseResponseValidator();

            _licenseService =
                LicenseService.Instance;

            _signedLicenseStorageService =
                new SignedLicenseStorageService();
        }

        public async Task<LicenseActivationResult>
            CheckActivationAsync(
                CancellationToken cancellationToken =
                    default)
        {
            PendingPurchaseInfo? pendingPurchase =
                _pendingPurchaseService
                    .CurrentPurchase;

            if (pendingPurchase == null ||
                !_pendingPurchaseService
                    .HasPendingPurchase)
            {
                return new LicenseActivationResult
                {
                    Status =
                        LicenseActivationStatus.Error,

                    Message =
                        "There is no pending purchase to verify."
                };
            }

            string deviceId =
                _deviceIdentityService
                    .GetDeviceId();

            var request =
                new LicenseActivationCheckRequest
                {
                    CustomerEmail =
                        pendingPurchase
                            .CustomerEmail,

                    PurchaseSessionId =
                        pendingPurchase
                            .PurchaseSessionId,

                    DeviceId =
                        deviceId,

                    ProductName =
                        LicenseSecurityConfiguration
                            .ProductName
                };

            LicenseActivationCheckResponse response =
                await _apiClient
                    .CheckAsync(
                        request,
                        cancellationToken);

            if (!response.Success)
            {
                return MapApiFailure(
                    response);
            }

            if (!response.PaymentCompleted)
            {
                return new LicenseActivationResult
                {
                    Status =
                        LicenseActivationStatus
                            .PaymentPending,

                    Message =
                        string.IsNullOrWhiteSpace(
                            response.Message)
                            ? "The payment has not been confirmed yet."
                            : response.Message
                };
            }

            if (response.License == null)
            {
                return new LicenseActivationResult
                {
                    Status =
                        LicenseActivationStatus.Error,

                    Message =
                        "The server confirmed payment but did not return a license."
                };
            }

            if (!string.Equals(
                    response.License.CustomerEmail,
                    pendingPurchase.CustomerEmail,
                    StringComparison.OrdinalIgnoreCase))
            {
                return new LicenseActivationResult
                {
                    Status =
                        LicenseActivationStatus.InvalidKey,

                    Message =
                        "The returned license belongs to another customer."
                };
            }

            if (response.License.Plan !=
                pendingPurchase.Plan)
            {
                return new LicenseActivationResult
                {
                    Status =
                        LicenseActivationStatus.InvalidKey,

                    Message =
                        "The returned license plan does not match the purchase."
                };
            }

            if (!LicenseSecurityConfiguration
                    .HasPublicKey)
            {
                return new LicenseActivationResult
                {
                    Status =
                        LicenseActivationStatus
                            .ServerUnavailable,

                    Message =
                        "The license verification key is not configured."
                };
            }

            LicenseActivationResult validationResult =
                _licenseResponseValidator
                    .Validate(
                        response.License,
                        LicenseSecurityConfiguration
                            .PublicKeyPem);

            if (!validationResult.IsSuccessful ||
                validationResult.License == null)
            {
                return validationResult;
            }

            try
            {
                _signedLicenseStorageService
                    .Save(
                        response.License);

                _licenseService.SetLicense(
                    validationResult.License);

                _pendingPurchaseService
                    .ClearPendingPurchase();
            }
            catch
            {
                return new LicenseActivationResult
                {
                    Status =
                        LicenseActivationStatus.Error,

                    Message =
                        "The activated license could not be saved locally."
                };
            }

            return validationResult;
        }

        private static LicenseActivationResult
            MapApiFailure(
                LicenseActivationCheckResponse response)
        {
            LicenseActivationStatus status =
                response.ErrorCode switch
                {
                    "SERVER_NOT_CONFIGURED" =>
                        LicenseActivationStatus
                            .ServerUnavailable,

                    "NETWORK_ERROR" =>
                        LicenseActivationStatus
                            .NetworkError,

                    "REQUEST_TIMEOUT" =>
                        LicenseActivationStatus
                            .NetworkError,

                    "PAYMENT_PENDING" =>
                        LicenseActivationStatus
                            .PaymentPending,

                    _ =>
                        LicenseActivationStatus
                            .Error
                };

            string message =
                string.IsNullOrWhiteSpace(
                    response.Message)
                    ? "The activation check failed."
                    : response.Message;

            return new LicenseActivationResult
            {
                Status =
                    status,

                Message =
                    message
            };
        }
    }
}