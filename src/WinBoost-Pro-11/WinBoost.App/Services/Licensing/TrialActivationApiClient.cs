using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using WinBoost.App.Models;

namespace WinBoost.App.Services.Licensing
{
    public sealed class TrialActivationApiClient
    {
        private readonly HttpClient
            _httpClient;

        public TrialActivationApiClient()
        {
            _httpClient =
                new HttpClient
                {
                    Timeout =
                        TimeSpan.FromSeconds(15)
                };
        }

        public async Task<TrialActivationResponse>
            ActivateTrialAsync(
                TrialActivationRequest request,
                CancellationToken cancellationToken =
                    default)
        {
            if (request == null)
            {
                return Error(
                    "INVALID_REQUEST",
                    "The trial activation request is invalid.");
            }

            string endpoint =
                LicenseSecurityConfiguration
                    .TrialActivationEndpoint;

            if (string.IsNullOrWhiteSpace(
                    endpoint))
            {
                return Error(
                    "ENDPOINT_UNAVAILABLE",
                    "The trial activation endpoint is unavailable.");
            }

            if (!Uri.TryCreate(
                    endpoint,
                    UriKind.Absolute,
                    out Uri? endpointUri) ||
                endpointUri.Scheme !=
                    Uri.UriSchemeHttps)
            {
                return Error(
                    "INVALID_ENDPOINT",
                    "The trial activation endpoint is invalid.");
            }

            try
            {
                using HttpResponseMessage response =
                    await _httpClient
                        .PostAsJsonAsync(
                            endpointUri,
                            request,
                            cancellationToken);

                TrialActivationResponse?
                    activationResponse =
                        await response.Content
                            .ReadFromJsonAsync
                                <TrialActivationResponse>(
                                    cancellationToken:
                                        cancellationToken);

                if (activationResponse != null)
                {
                    return activationResponse;
                }

                return Error(
                    "INVALID_RESPONSE",
                    "The trial activation server returned an invalid response.");
            }
            catch (OperationCanceledException)
                when (!cancellationToken
                    .IsCancellationRequested)
            {
                return Error(
                    "REQUEST_TIMEOUT",
                    "The trial activation request timed out.");
            }
            catch (HttpRequestException)
            {
                return Error(
                    "NETWORK_ERROR",
                    "The trial activation server could not be reached.");
            }
            catch
            {
                return Error(
                    "UNKNOWN_ERROR",
                    "The trial activation request failed.");
            }
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