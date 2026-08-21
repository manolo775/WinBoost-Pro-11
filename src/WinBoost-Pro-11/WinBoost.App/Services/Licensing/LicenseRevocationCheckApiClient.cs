using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using WinBoost.App.Models;

namespace WinBoost.App.Services.Licensing
{
    public sealed class LicenseRevocationCheckApiClient
    {
        private readonly HttpClient
            _httpClient;

        private readonly Uri?
            _revocationCheckEndpoint;

        public LicenseRevocationCheckApiClient(
            string? revocationCheckEndpoint = null)
        {
            _httpClient =
                new HttpClient
                {
                    Timeout =
                        TimeSpan.FromSeconds(15)
                };

            if (Uri.TryCreate(
                    revocationCheckEndpoint,
                    UriKind.Absolute,
                    out Uri? endpoint) &&
                endpoint.Scheme ==
                    Uri.UriSchemeHttps)
            {
                _revocationCheckEndpoint =
                    endpoint;
            }
        }

        public async Task<LicenseRevocationCheckResponse>
            CheckAsync(
                LicenseRevocationCheckRequest request,
                CancellationToken cancellationToken =
                    default)
        {
            ArgumentNullException.ThrowIfNull(
                request);

            if (_revocationCheckEndpoint == null)
            {
                return new LicenseRevocationCheckResponse
                {
                    Success =
                        false,

                    IsRevoked =
                        false,

                    ErrorCode =
                        "SERVER_NOT_CONFIGURED",

                    Message =
                        "WinBoost licensing server is not configured."
                };
            }

            try
            {
                using HttpResponseMessage httpResponse =
                    await _httpClient
                        .PostAsJsonAsync(
                            _revocationCheckEndpoint,
                            request,
                            cancellationToken);

                LicenseRevocationCheckResponse?
                    response =
                        await httpResponse.Content
                            .ReadFromJsonAsync<
                                LicenseRevocationCheckResponse>(
                                cancellationToken:
                                    cancellationToken);

                if (response != null)
                {
                    return response;
                }

                return new LicenseRevocationCheckResponse
                {
                    Success =
                        false,

                    IsRevoked =
                        false,

                    ErrorCode =
                        "INVALID_SERVER_RESPONSE",

                    Message =
                        "The licensing server returned an invalid revocation response."
                };
            }
            catch (HttpRequestException)
            {
                return new LicenseRevocationCheckResponse
                {
                    Success =
                        false,

                    IsRevoked =
                        false,

                    ErrorCode =
                        "NETWORK_ERROR",

                    Message =
                        "The licensing server could not be reached."
                };
            }
            catch (TaskCanceledException)
            {
                return new LicenseRevocationCheckResponse
                {
                    Success =
                        false,

                    IsRevoked =
                        false,

                    ErrorCode =
                        "REQUEST_TIMEOUT",

                    Message =
                        "The licensing request timed out."
                };
            }
            catch (Exception)
            {
                return new LicenseRevocationCheckResponse
                {
                    Success =
                        false,

                    IsRevoked =
                        false,

                    ErrorCode =
                        "UNKNOWN_ERROR",

                    Message =
                        "An unexpected licensing error occurred."
                };
            }
        }
    }
}