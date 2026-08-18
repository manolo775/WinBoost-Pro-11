using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using WinBoost.App.Models;

namespace WinBoost.App.Services.Licensing
{
    public sealed class LicenseApiClient
    {
        private readonly HttpClient
            _httpClient;

        private readonly Uri?
            _activationEndpoint;

        public LicenseApiClient(
            string? activationEndpoint = null)
        {
            _httpClient =
                new HttpClient
                {
                    Timeout =
                        TimeSpan.FromSeconds(15)
                };

            if (Uri.TryCreate(
                    activationEndpoint,
                    UriKind.Absolute,
                    out Uri? endpoint) &&
                endpoint.Scheme ==
                    Uri.UriSchemeHttps)
            {
                _activationEndpoint =
                    endpoint;
            }
        }

        public async Task<LicenseActivationApiResponse>
            ActivateAsync(
                LicenseActivationRequest request,
                CancellationToken cancellationToken =
                    default)
        {
            ArgumentNullException.ThrowIfNull(
                request);

            if (_activationEndpoint == null)
            {
                return new LicenseActivationApiResponse
                {
                    Success =
                        false,

                    ErrorCode =
                        "SERVER_NOT_CONFIGURED",

                    Message =
                        "WinBoost licensing server is not configured."
                };
            }

            try
            {
                using HttpResponseMessage response =
                    await _httpClient.PostAsJsonAsync(
                        _activationEndpoint,
                        request,
                        cancellationToken);

                LicenseActivationApiResponse?
                    apiResponse =
                        await response.Content
                            .ReadFromJsonAsync<
                                LicenseActivationApiResponse>(
                                cancellationToken:
                                    cancellationToken);

                if (apiResponse != null)
                {
                    return apiResponse;
                }

                return new LicenseActivationApiResponse
                {
                    Success =
                        false,

                    ErrorCode =
                        "INVALID_SERVER_RESPONSE",

                    Message =
                        "The licensing server returned an invalid response."
                };
            }
            catch (HttpRequestException)
            {
                return new LicenseActivationApiResponse
                {
                    Success =
                        false,

                    ErrorCode =
                        "NETWORK_ERROR",

                    Message =
                        "The licensing server could not be reached."
                };
            }
            catch (TaskCanceledException)
            {
                return new LicenseActivationApiResponse
                {
                    Success =
                        false,

                    ErrorCode =
                        "REQUEST_TIMEOUT",

                    Message =
                        "The licensing request timed out."
                };
            }
            catch (Exception)
            {
                return new LicenseActivationApiResponse
                {
                    Success =
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