using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using WinBoost.App.Models;

namespace WinBoost.App.Services.Licensing
{
    public sealed class LicenseActivationCheckApiClient
    {
        private readonly HttpClient
            _httpClient;

        private readonly Uri?
            _activationCheckEndpoint;

        public LicenseActivationCheckApiClient(
            string? activationCheckEndpoint = null)
        {
            _httpClient =
                new HttpClient
                {
                    Timeout =
                        TimeSpan.FromSeconds(15)
                };

            if (Uri.TryCreate(
                    activationCheckEndpoint,
                    UriKind.Absolute,
                    out Uri? endpoint) &&
                endpoint.Scheme ==
                    Uri.UriSchemeHttps)
            {
                _activationCheckEndpoint =
                    endpoint;
            }
        }

        public async Task<LicenseActivationCheckResponse>
            CheckAsync(
                LicenseActivationCheckRequest request,
                CancellationToken cancellationToken =
                    default)
        {
            ArgumentNullException.ThrowIfNull(
                request);

            if (_activationCheckEndpoint == null)
            {
                return new LicenseActivationCheckResponse
                {
                    Success =
                        false,

                    PaymentCompleted =
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
                            _activationCheckEndpoint,
                            request,
                            cancellationToken);

                LicenseActivationCheckResponse?
                    response =
                        await httpResponse.Content
                            .ReadFromJsonAsync<
                                LicenseActivationCheckResponse>(
                                cancellationToken:
                                    cancellationToken);

                if (response != null)
                {
                    return response;
                }

                return new LicenseActivationCheckResponse
                {
                    Success =
                        false,

                    PaymentCompleted =
                        false,

                    ErrorCode =
                        "INVALID_SERVER_RESPONSE",

                    Message =
                        "The licensing server returned an invalid activation response."
                };
            }
            catch (HttpRequestException)
            {
                return new LicenseActivationCheckResponse
                {
                    Success =
                        false,

                    PaymentCompleted =
                        false,

                    ErrorCode =
                        "NETWORK_ERROR",

                    Message =
                        "The licensing server could not be reached."
                };
            }
            catch (TaskCanceledException)
            {
                return new LicenseActivationCheckResponse
                {
                    Success =
                        false,

                    PaymentCompleted =
                        false,

                    ErrorCode =
                        "REQUEST_TIMEOUT",

                    Message =
                        "The licensing request timed out."
                };
            }
            catch (Exception)
            {
                return new LicenseActivationCheckResponse
                {
                    Success =
                        false,

                    PaymentCompleted =
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