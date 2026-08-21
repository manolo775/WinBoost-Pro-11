using System;
using System.Threading;
using System.Threading.Tasks;
using WinBoost.App.Models;

namespace WinBoost.App.Services.Licensing
{
    public sealed class TrialActivationService
    {
        private const string ProductName =
            "WinBoost Pro 11";

        private readonly DeviceIdentityService
            _deviceIdentityService;

        private readonly TrialDeviceTokenService
            _trialDeviceTokenService;

        private readonly TrialActivationApiClient
            _trialActivationApiClient;

        private readonly LicenseResponseValidator
            _licenseResponseValidator;

        private readonly SignedLicenseStorageService
            _signedLicenseStorageService;

        private readonly LicenseService
            _licenseService;

        public TrialActivationService()
        {
            _deviceIdentityService =
                new DeviceIdentityService();

            _trialDeviceTokenService =
                new TrialDeviceTokenService();

            _trialActivationApiClient =
                new TrialActivationApiClient();

            _licenseResponseValidator =
                new LicenseResponseValidator();

            _signedLicenseStorageService =
                new SignedLicenseStorageService();

            _licenseService =
                LicenseService.Instance;
        }

        public async Task<TrialActivationResponse>
            ActivateTrialAsync(
                CancellationToken cancellationToken =
                    default)
        {
            string deviceId =
                _deviceIdentityService
                    .GetDeviceId();

            if (string.IsNullOrWhiteSpace(
                    deviceId))
            {
                return Error(
                    "DEVICE_ID_UNAVAILABLE",
                    "The device identifier is unavailable.");
            }

            string trialDeviceToken =
                _trialDeviceTokenService
                    .GetTrialDeviceToken();

            if (string.IsNullOrWhiteSpace(
                    trialDeviceToken))
            {
                return Error(
                    "TRIAL_DEVICE_UNAVAILABLE",
                    "The trial device identifier is unavailable.");
            }

            var request =
                new TrialActivationRequest
                {
                    DeviceId =
                        deviceId,

                    TrialDeviceToken =
                        trialDeviceToken,

                    ProductName =
                        ProductName
                };

            TrialActivationResponse response =
                await _trialActivationApiClient
                    .ActivateTrialAsync(
                        request,
                        cancellationToken);

            if (!response.Success ||
                response.License == null)
            {
                return response;
            }

            if (!LicenseSecurityConfiguration
                    .HasPublicKey)
            {
                return Error(
                    "PUBLIC_KEY_UNAVAILABLE",
                    "The license verification key is unavailable.");
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
                return Error(
                    "INVALID_TRIAL_LICENSE",
                    "The trial license returned by the server is invalid.");
            }

            if (validationResult.License.Status !=
                    LicenseStatus.Trial ||
                validationResult.License.Plan !=
                    LicensePlan.Trial)
            {
                return Error(
                    "INVALID_TRIAL_LICENSE",
                    "The server did not return a valid trial license.");
            }

            try
            {
                _signedLicenseStorageService
                    .Save(
                        response.License);

                _licenseService
                    .ReloadLicense();
            }
            catch
            {
                return Error(
                    "LICENSE_STORAGE_ERROR",
                    "The trial license could not be stored.");
            }

            if (_licenseService.Status !=
                    LicenseStatus.Trial ||
                !_licenseService.IsActive)
            {
                return Error(
                    "TRIAL_ACTIVATION_FAILED",
                    "The trial license could not be activated.");
            }

            return response;
        }

        private static TrialActivationResponse
            Error(
                string errorCode,
                string message)
        {
            return new TrialActivationResponse
            {
                Success =
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