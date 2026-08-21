using System.Threading;
using System.Threading.Tasks;
using WinBoost.App.Models;

namespace WinBoost.App.Services.Licensing
{
    public sealed class LicenseRevocationService
    {
        private readonly LicenseService
            _licenseService;

        private readonly DeviceIdentityService
            _deviceIdentityService;

        private readonly LicenseRevocationCheckApiClient
            _apiClient;

        public LicenseRevocationService()
        {
            _licenseService =
                LicenseService.Instance;

            _deviceIdentityService =
                new DeviceIdentityService();

            _apiClient =
                new LicenseRevocationCheckApiClient(
                    LicenseSecurityConfiguration
                        .RevocationCheckEndpoint);
        }

        public async Task<LicenseRevocationCheckResponse>
            CheckCurrentLicenseAsync(
                CancellationToken cancellationToken =
                    default)
        {
            LicenseInfo license =
                _licenseService
                    .CurrentLicense;

            if (license.Status !=
                    LicenseStatus.Licensed &&
                license.Status !=
                    LicenseStatus.Trial)
            {
                return new LicenseRevocationCheckResponse
                {
                    Success =
                        true,

                    IsRevoked =
                        false,

                    ErrorCode =
                        string.Empty,

                    Message =
                        "There is no active license that requires a revocation check."
                };
            }

            string licenseId =
                license.LicenseId?.Trim()
                ?? string.Empty;

            if (string.IsNullOrWhiteSpace(
                    licenseId))
            {
                return new LicenseRevocationCheckResponse
                {
                    Success =
                        false,

                    IsRevoked =
                        false,

                    ErrorCode =
                        "LICENSE_ID_MISSING",

                    Message =
                        "The active license does not contain a license identifier."
                };
            }

            var request =
                new LicenseRevocationCheckRequest
                {
                    LicenseId =
                        licenseId,

                    DeviceId =
                        _deviceIdentityService
                            .GetDeviceId(),

                    ProductName =
                        LicenseSecurityConfiguration
                            .ProductName
                };

            LicenseRevocationCheckResponse response =
                await _apiClient
                    .CheckAsync(
                        request,
                        cancellationToken);

            if (!response.Success)
            {
                // Server unavailable, network error,
                // timeout, or another server-side error.
                //
                // The existing signed offline license
                // remains valid.
                return response;
            }

            if (!response.IsRevoked)
            {
                return response;
            }

            // The server explicitly confirmed that
            // this license is revoked.
            //
            // Remove both the local cache and the
            // signed offline license so that it cannot
            // continue to be used after restart.
            _licenseService
                .ClearLicense();

            return response;
        }
    }
}