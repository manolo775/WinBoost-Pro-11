using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using WinBoost.App.Models;

namespace WinBoost.App.Services.Licensing
{
    public sealed class PurchaseApiClient
    {
        private readonly HttpClient
            _httpClient;

        private readonly Uri?
            _purchaseSessionEndpoint;

        public PurchaseApiClient(
            string? purchaseSessionEndpoint = null)
        {
            _httpClient =
                new HttpClient
                {
                    Timeout =
                        TimeSpan.FromSeconds(15)
                };

            if (Uri.TryCreate(
                    purchaseSessionEndpoint,
                    UriKind.Absolute,
                    out Uri? endpoint) &&
                endpoint.Scheme ==
                    Uri.UriSchemeHttps)
            {
                _purchaseSessionEndpoint =
                    endpoint;
            }
        }

        public async Task<PurchaseSessionResponse>
            CreateSessionAsync(
                PurchaseSessionRequest request,
                CancellationToken cancellationToken =
                    default)
        {
            ArgumentNullException.ThrowIfNull(
                request);

            if (_purchaseSessionEndpoint == null)
            {
                return new PurchaseSessionResponse
                {
                    Success =
                        false,

                    ErrorCode =
                        "SERVER_NOT_CONFIGURED",

                    Message =
                        "WinBoost purchase server is not configured."
                };
            }

            try
            {
                using HttpResponseMessage response =
                    await _httpClient.PostAsJsonAsync(
                        _purchaseSessionEndpoint,
                        request,
                        cancellationToken);

                PurchaseSessionResponse?
                    apiResponse =
                        await response.Content
                            .ReadFromJsonAsync<
                                PurchaseSessionResponse>(
                                cancellationToken:
                                    cancellationToken);

                if (apiResponse != null)
                {
                    return apiResponse;
                }

                return new PurchaseSessionResponse
                {
                    Success =
                        false,

                    ErrorCode =
                        "INVALID_SERVER_RESPONSE",

                    Message =
                        "The purchase server returned an invalid response."
                };
            }
            catch (HttpRequestException)
            {
                return new PurchaseSessionResponse
                {
                    Success =
                        false,

                    ErrorCode =
                        "NETWORK_ERROR",

                    Message =
                        "The purchase server could not be reached."
                };
            }
            catch (TaskCanceledException)
            {
                return new PurchaseSessionResponse
                {
                    Success =
                        false,

                    ErrorCode =
                        "REQUEST_TIMEOUT",

                    Message =
                        "The purchase request timed out."
                };
            }
            catch (Exception)
            {
                return new PurchaseSessionResponse
                {
                    Success =
                        false,

                    ErrorCode =
                        "UNKNOWN_ERROR",

                    Message =
                        "An unexpected purchase error occurred."
                };
            }
        }
    }
}