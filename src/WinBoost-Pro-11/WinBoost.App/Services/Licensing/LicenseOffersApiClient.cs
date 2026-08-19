using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using WinBoost.App.Models;

namespace WinBoost.App.Services.Licensing
{
    public sealed class LicenseOffersApiClient
    {
        private readonly HttpClient
            _httpClient;

        private readonly Uri?
            _offersEndpoint;

        public LicenseOffersApiClient(
            string? offersEndpoint = null)
        {
            _httpClient =
                new HttpClient
                {
                    Timeout =
                        TimeSpan.FromSeconds(15)
                };

            if (Uri.TryCreate(
                    offersEndpoint,
                    UriKind.Absolute,
                    out Uri? endpoint) &&
                endpoint.Scheme ==
                    Uri.UriSchemeHttps)
            {
                _offersEndpoint =
                    endpoint;
            }
        }

        public async Task<LicenseOffersResponse>
            GetOffersAsync(
                CancellationToken cancellationToken =
                    default)
        {
            if (_offersEndpoint == null)
            {
                return new LicenseOffersResponse
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
                LicenseOffersResponse?
                    response =
                        await _httpClient
                            .GetFromJsonAsync<
                                LicenseOffersResponse>(
                                _offersEndpoint,
                                cancellationToken);

                if (response != null)
                {
                    return response;
                }

                return new LicenseOffersResponse
                {
                    Success =
                        false,

                    ErrorCode =
                        "INVALID_SERVER_RESPONSE",

                    Message =
                        "The licensing server returned an invalid offers response."
                };
            }
            catch (HttpRequestException)
            {
                return new LicenseOffersResponse
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
                return new LicenseOffersResponse
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
                return new LicenseOffersResponse
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