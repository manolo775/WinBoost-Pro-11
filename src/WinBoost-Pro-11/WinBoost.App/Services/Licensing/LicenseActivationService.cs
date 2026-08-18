using System;
using System.Threading.Tasks;
using WinBoost.App.Models;

namespace WinBoost.App.Services.Licensing
{
    public sealed class LicenseActivationService
    {
        private readonly DeviceIdentityService
            _deviceIdentityService;

        private readonly LicenseResponseValidator
            _responseValidator;

        private readonly LicenseApiClient
            _apiClient;

        public LicenseActivationService()
        {
            _deviceIdentityService =
                new DeviceIdentityService();

            _responseValidator =
                new LicenseResponseValidator();

            _apiClient =
                new LicenseApiClient(
                    LicenseSecurityConfiguration
                        .ActivationEndpoint);
        }

        public Task<LicenseActivationResult>
            ActivateAsync(
                string customerEmail,
                string activationToken)
        {
            var request =
                new LicenseActivationRequest
                {
                    CustomerEmail =
                        customerEmail?.Trim()
                        ?? string.Empty,

                    ActivationToken =
                        activationToken?.Trim()
                        ?? string.Empty,

                    DeviceId =
                        _deviceIdentityService
                            .GetDeviceId(),

                    ProductName =
                        LicenseSecurityConfiguration
                            .ProductName
                };

            return ActivateAsync(
                request);
        }

        public LicenseActivationResult
            ValidateServerResponse(
                SignedLicenseResponse response)
        {
            ArgumentNullException.ThrowIfNull(
                response);

            if (!LicenseSecurityConfiguration
                    .HasPublicKey)
            {
                return new LicenseActivationResult
                {
                    Status =
                        LicenseActivationStatus
                            .ServerUnavailable,

                    Message =
                        "WinBoost licensing public key is not configured."
                };
            }

            return _responseValidator.Validate(
                response,
                LicenseSecurityConfiguration
                    .PublicKeyPem);
        }

        public async Task<LicenseActivationResult>
            ActivateAsync(
                LicenseActivationRequest request)
        {
            ArgumentNullException.ThrowIfNull(
                request);

            if (string.IsNullOrWhiteSpace(
                    request.CustomerEmail))
            {
                return new LicenseActivationResult
                {
                    Status =
                        LicenseActivationStatus.InvalidKey,

                    Message =
                        "Customer email is required."
                };
            }

            if (string.IsNullOrWhiteSpace(
                    request.ActivationToken))
            {
                return new LicenseActivationResult
                {
                    Status =
                        LicenseActivationStatus.InvalidKey,

                    Message =
                        "Activation token is required."
                };
            }

            if (string.IsNullOrWhiteSpace(
                    request.DeviceId))
            {
                return new LicenseActivationResult
                {
                    Status =
                        LicenseActivationStatus.Error,

                    Message =
                        "Device identifier is required."
                };
            }

            LicenseActivationApiResponse
                apiResponse =
                    await _apiClient.ActivateAsync(
                        request);

            if (!apiResponse.Success)
            {
                return MapApiError(
                    apiResponse);
            }

            if (apiResponse.License == null)
            {
                return new LicenseActivationResult
                {
                    Status =
                        LicenseActivationStatus.Error,

                    Message =
                        "The licensing server did not return a license."
                };
            }

            return ValidateServerResponse(
                apiResponse.License);
        }

        private static LicenseActivationResult
            MapApiError(
                LicenseActivationApiResponse
                    apiResponse)
        {
            LicenseActivationStatus status =
                apiResponse.ErrorCode switch
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

                    "LICENSE_EXPIRED" =>
                        LicenseActivationStatus
                            .Expired,

                    "ALREADY_ACTIVATED" =>
                        LicenseActivationStatus
                            .AlreadyActivated,

                    "INVALID_TOKEN" =>
                        LicenseActivationStatus
                            .InvalidKey,

                    "PAYMENT_NOT_FOUND" =>
                        LicenseActivationStatus
                            .InvalidKey,

                    _ =>
                        LicenseActivationStatus
                            .Error
                };

            return new LicenseActivationResult
            {
                Status =
                    status,

                Message =
                    apiResponse.Message
            };
        }
    }
}