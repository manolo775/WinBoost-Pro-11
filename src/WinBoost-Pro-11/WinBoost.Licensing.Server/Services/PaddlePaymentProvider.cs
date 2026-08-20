using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using WinBoost.Licensing.Server.Configuration;
using WinBoost.Licensing.Server.Models;

namespace WinBoost.Licensing.Server.Services
{
    public sealed class PaddlePaymentProvider
        : IPaymentProvider
    {
        private readonly HttpClient
            _httpClient;

        private readonly PaddleOptions
            _options;

        public PaddlePaymentProvider(
            HttpClient httpClient,
            IOptions<PaddleOptions> options)
        {
            _httpClient =
                httpClient;

            _options =
                options.Value;
        }

        public async Task<PaymentCheckoutResult>
            CreateCheckoutAsync(
                PurchaseSessionRequest request,
                CancellationToken cancellationToken =
                    default)
        {
            if (string.IsNullOrWhiteSpace(
                    _options.ApiKey))
            {
                return CheckoutError(
                    "PAYMENT_PROVIDER_NOT_CONFIGURED",
                    "The Paddle API key is not configured.");
            }

            string priceId =
                GetPriceId(
                    request.Plan);

            if (string.IsNullOrWhiteSpace(
                    priceId))
            {
                return CheckoutError(
                    "PADDLE_PRICE_NOT_CONFIGURED",
                    "The Paddle price is not configured for this license plan.");
            }

            if (!IsValidBaseUrl())
            {
                return CheckoutError(
                    "PAYMENT_PROVIDER_NOT_CONFIGURED",
                    "The Paddle API URL is invalid.");
            }

            PaymentCustomerResult customer =
                await GetOrCreateCustomerAsync(
                    request.CustomerEmail,
                    cancellationToken);

            if (!customer.Success)
            {
                return CheckoutError(
                    customer.ErrorCode,
                    customer.Message);
            }

            string endpoint =
                $"{_options.BaseUrl.TrimEnd('/')}/transactions";

            var payload =
                new
                {
                    items =
                        new[]
                        {
                            new
                            {
                                price_id =
                                    priceId,

                                quantity =
                                    1
                            }
                        },

                    customer_id =
                        customer.CustomerId,

                    collection_mode =
                        "automatic",

                    custom_data =
                        new Dictionary<string, string>
                        {
                            ["winboost_email"] =
                                customer.Email,

                            ["winboost_device_id"] =
                                request.DeviceId,

                            ["winboost_product"] =
                                request.ProductName,

                            ["winboost_plan"] =
                                request.Plan.ToString()
                        }
                };

            using var httpRequest =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    endpoint);

            AddAuthorization(
                httpRequest);

            httpRequest.Content =
                JsonContent.Create(
                    payload);

            try
            {
                using HttpResponseMessage response =
                    await _httpClient
                        .SendAsync(
                            httpRequest,
                            cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return CheckoutError(
                        "PADDLE_API_ERROR",
                        $"Paddle returned HTTP {(int)response.StatusCode}.");
                }

                PaddleTransactionResponse? result =
                    await response.Content
                        .ReadFromJsonAsync<
                            PaddleTransactionResponse>(
                                cancellationToken:
                                    cancellationToken);

                string transactionId =
                    result?.Data?.Id
                    ?? string.Empty;

                string checkoutUrl =
                    result?.Data?.Checkout?.Url
                    ?? string.Empty;

                if (string.IsNullOrWhiteSpace(
                        transactionId))
                {
                    return CheckoutError(
                        "PADDLE_INVALID_RESPONSE",
                        "Paddle did not return a transaction identifier.");
                }

                if (string.IsNullOrWhiteSpace(
                        checkoutUrl))
                {
                    return CheckoutError(
                        "PADDLE_INVALID_RESPONSE",
                        "Paddle did not return a checkout URL.");
                }

                return new PaymentCheckoutResult
                {
                    Success =
                        true,

                    ProviderSessionId =
                        transactionId,

                    CheckoutUrl =
                        checkoutUrl,

                    ErrorCode =
                        string.Empty,

                    Message =
                        string.Empty
                };
            }
            catch (OperationCanceledException)
                when (!cancellationToken
                    .IsCancellationRequested)
            {
                return CheckoutError(
                    "PADDLE_TIMEOUT",
                    "The Paddle request timed out.");
            }
            catch (HttpRequestException)
            {
                return CheckoutError(
                    "PADDLE_NETWORK_ERROR",
                    "The Paddle API could not be reached.");
            }
            catch
            {
                return CheckoutError(
                    "PADDLE_ERROR",
                    "An unexpected Paddle error occurred.");
            }
        }

        public async Task<PaymentTransactionStatusResult>
            GetTransactionStatusAsync(
                string providerSessionId,
                CancellationToken cancellationToken =
                    default)
        {
            if (string.IsNullOrWhiteSpace(
                    _options.ApiKey))
            {
                return StatusError(
                    "PAYMENT_PROVIDER_NOT_CONFIGURED",
                    "The Paddle API key is not configured.");
            }

            if (string.IsNullOrWhiteSpace(
                    providerSessionId) ||
                !providerSessionId.StartsWith(
                    "txn_",
                    StringComparison.Ordinal))
            {
                return StatusError(
                    "INVALID_TRANSACTION_ID",
                    "The Paddle transaction identifier is invalid.");
            }

            if (!IsValidBaseUrl())
            {
                return StatusError(
                    "PAYMENT_PROVIDER_NOT_CONFIGURED",
                    "The Paddle API URL is invalid.");
            }

            string transactionId =
                Uri.EscapeDataString(
                    providerSessionId);

            string endpoint =
                $"{_options.BaseUrl.TrimEnd('/')}/transactions/{transactionId}";

            using var httpRequest =
                new HttpRequestMessage(
                    HttpMethod.Get,
                    endpoint);

            AddAuthorization(
                httpRequest);

            try
            {
                using HttpResponseMessage response =
                    await _httpClient
                        .SendAsync(
                            httpRequest,
                            cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return StatusError(
                        "PADDLE_API_ERROR",
                        $"Paddle returned HTTP {(int)response.StatusCode}.");
                }

                PaddleTransactionStatusResponse? result =
                    await response.Content
                        .ReadFromJsonAsync<
                            PaddleTransactionStatusResponse>(
                                cancellationToken:
                                    cancellationToken);

                PaddleTransactionStatusData? data =
                    result?.Data;

                if (data == null ||
                    string.IsNullOrWhiteSpace(
                        data.Id))
                {
                    return StatusError(
                        "PADDLE_INVALID_RESPONSE",
                        "Paddle did not return transaction data.");
                }

                if (string.IsNullOrWhiteSpace(
                        data.CustomerId))
                {
                    return StatusError(
                        "PADDLE_CUSTOMER_NOT_FOUND",
                        "The Paddle transaction is not associated with a customer.");
                }

                PaymentCustomerResult customer =
                    await GetCustomerByIdAsync(
                        data.CustomerId,
                        cancellationToken);

                if (!customer.Success)
                {
                    return StatusError(
                        customer.ErrorCode,
                        customer.Message);
                }

                string metadataEmail =
                    GetCustomDataValue(
                        data.CustomData,
                        "winboost_email");

                if (!string.Equals(
                        metadataEmail,
                        customer.Email,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return StatusError(
                        "PADDLE_CUSTOMER_EMAIL_MISMATCH",
                        "The Paddle customer email does not match the WinBoost purchase email.");
                }

                string deviceId =
                    GetCustomDataValue(
                        data.CustomData,
                        "winboost_device_id");

                string productName =
                    GetCustomDataValue(
                        data.CustomData,
                        "winboost_product");

                string planText =
                    GetCustomDataValue(
                        data.CustomData,
                        "winboost_plan");

                LicensePlan plan =
                    LicensePlan.Unknown;

                Enum.TryParse(
                    planText,
                    false,
                    out plan);

                string priceId =
                    string.Empty;

                if (data.Items != null &&
                    data.Items.Count > 0 &&
                    data.Items[0].Price != null)
                {
                    priceId =
                        data.Items[0]
                            .Price!
                            .Id;
                }

                bool paymentCompleted =
                    string.Equals(
                        data.Status,
                        "completed",
                        StringComparison.OrdinalIgnoreCase);

                return new PaymentTransactionStatusResult
                {
                    Success =
                        true,

                    ProviderSessionId =
                        data.Id,

                    Status =
                        data.Status,

                    PaymentCompleted =
                        paymentCompleted,

                    PriceId =
                        priceId,

                    CustomerEmail =
                        customer.Email,

                    DeviceId =
                        deviceId,

                    ProductName =
                        productName,

                    Plan =
                        plan,

                    ErrorCode =
                        string.Empty,

                    Message =
                        string.Empty
                };
            }
            catch (OperationCanceledException)
                when (!cancellationToken
                    .IsCancellationRequested)
            {
                return StatusError(
                    "PADDLE_TIMEOUT",
                    "The Paddle request timed out.");
            }
            catch (HttpRequestException)
            {
                return StatusError(
                    "PADDLE_NETWORK_ERROR",
                    "The Paddle API could not be reached.");
            }
            catch
            {
                return StatusError(
                    "PADDLE_ERROR",
                    "An unexpected Paddle error occurred.");
            }
        }

        private async Task<PaymentCustomerResult>
            GetOrCreateCustomerAsync(
                string customerEmail,
                CancellationToken cancellationToken)
        {
            string email =
                customerEmail?.Trim()
                ?? string.Empty;

            if (string.IsNullOrWhiteSpace(
                    email))
            {
                return CustomerError(
                    "INVALID_EMAIL",
                    "The Paddle customer email is invalid.");
            }

            string encodedEmail =
                Uri.EscapeDataString(
                    email);

            string listEndpoint =
                $"{_options.BaseUrl.TrimEnd('/')}/customers?email={encodedEmail}&per_page=1";

            using var listRequest =
                new HttpRequestMessage(
                    HttpMethod.Get,
                    listEndpoint);

            AddAuthorization(
                listRequest);

            try
            {
                using HttpResponseMessage listResponse =
                    await _httpClient
                        .SendAsync(
                            listRequest,
                            cancellationToken);

                if (!listResponse.IsSuccessStatusCode)
                {
                    return CustomerError(
                        "PADDLE_CUSTOMER_API_ERROR",
                        $"Paddle returned HTTP {(int)listResponse.StatusCode} while searching for the customer.");
                }

                PaddleCustomerListResponse? customers =
                    await listResponse.Content
                        .ReadFromJsonAsync<
                            PaddleCustomerListResponse>(
                                cancellationToken:
                                    cancellationToken);

                if (customers?.Data != null &&
                    customers.Data.Count > 0)
                {
                    PaddleCustomerData existing =
                        customers.Data[0];

                    if (string.IsNullOrWhiteSpace(
                            existing.Id) ||
                        string.IsNullOrWhiteSpace(
                            existing.Email))
                    {
                        return CustomerError(
                            "PADDLE_INVALID_CUSTOMER",
                            "Paddle returned invalid customer data.");
                    }

                    if (!string.Equals(
                            existing.Email,
                            email,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return CustomerError(
                            "PADDLE_CUSTOMER_EMAIL_MISMATCH",
                            "The Paddle customer email does not match the requested email.");
                    }

                    return new PaymentCustomerResult
                    {
                        Success =
                            true,

                        CustomerId =
                            existing.Id,

                        Email =
                            existing.Email,

                        ErrorCode =
                            string.Empty,

                        Message =
                            string.Empty
                    };
                }

                return await CreateCustomerAsync(
                    email,
                    cancellationToken);
            }
            catch (OperationCanceledException)
                when (!cancellationToken
                    .IsCancellationRequested)
            {
                return CustomerError(
                    "PADDLE_TIMEOUT",
                    "The Paddle customer request timed out.");
            }
            catch (HttpRequestException)
            {
                return CustomerError(
                    "PADDLE_NETWORK_ERROR",
                    "The Paddle API could not be reached.");
            }
            catch
            {
                return CustomerError(
                    "PADDLE_CUSTOMER_ERROR",
                    "An unexpected Paddle customer error occurred.");
            }
        }

        private async Task<PaymentCustomerResult>
            CreateCustomerAsync(
                string email,
                CancellationToken cancellationToken)
        {
            string endpoint =
                $"{_options.BaseUrl.TrimEnd('/')}/customers";

            var payload =
                new
                {
                    email =
                        email
                };

            using var httpRequest =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    endpoint);

            AddAuthorization(
                httpRequest);

            httpRequest.Content =
                JsonContent.Create(
                    payload);

            try
            {
                using HttpResponseMessage response =
                    await _httpClient
                        .SendAsync(
                            httpRequest,
                            cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return CustomerError(
                        "PADDLE_CUSTOMER_API_ERROR",
                        $"Paddle returned HTTP {(int)response.StatusCode} while creating the customer.");
                }

                PaddleCustomerResponse? result =
                    await response.Content
                        .ReadFromJsonAsync<
                            PaddleCustomerResponse>(
                                cancellationToken:
                                    cancellationToken);

                PaddleCustomerData? customer =
                    result?.Data;

                if (customer == null ||
                    string.IsNullOrWhiteSpace(
                        customer.Id) ||
                    string.IsNullOrWhiteSpace(
                        customer.Email))
                {
                    return CustomerError(
                        "PADDLE_INVALID_CUSTOMER",
                        "Paddle did not return valid customer data.");
                }

                if (!string.Equals(
                        customer.Email,
                        email,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return CustomerError(
                        "PADDLE_CUSTOMER_EMAIL_MISMATCH",
                        "The created Paddle customer email does not match the requested email.");
                }

                return new PaymentCustomerResult
                {
                    Success =
                        true,

                    CustomerId =
                        customer.Id,

                    Email =
                        customer.Email,

                    ErrorCode =
                        string.Empty,

                    Message =
                        string.Empty
                };
            }
            catch (OperationCanceledException)
                when (!cancellationToken
                    .IsCancellationRequested)
            {
                return CustomerError(
                    "PADDLE_TIMEOUT",
                    "The Paddle customer request timed out.");
            }
            catch (HttpRequestException)
            {
                return CustomerError(
                    "PADDLE_NETWORK_ERROR",
                    "The Paddle API could not be reached.");
            }
            catch
            {
                return CustomerError(
                    "PADDLE_CUSTOMER_ERROR",
                    "An unexpected Paddle customer error occurred.");
            }
        }

        private async Task<PaymentCustomerResult>
            GetCustomerByIdAsync(
                string customerId,
                CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(
                    customerId) ||
                !customerId.StartsWith(
                    "ctm_",
                    StringComparison.Ordinal))
            {
                return CustomerError(
                    "INVALID_CUSTOMER_ID",
                    "The Paddle customer identifier is invalid.");
            }

            string encodedCustomerId =
                Uri.EscapeDataString(
                    customerId);

            string endpoint =
                $"{_options.BaseUrl.TrimEnd('/')}/customers/{encodedCustomerId}";

            using var httpRequest =
                new HttpRequestMessage(
                    HttpMethod.Get,
                    endpoint);

            AddAuthorization(
                httpRequest);

            try
            {
                using HttpResponseMessage response =
                    await _httpClient
                        .SendAsync(
                            httpRequest,
                            cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return CustomerError(
                        "PADDLE_CUSTOMER_API_ERROR",
                        $"Paddle returned HTTP {(int)response.StatusCode} while reading the customer.");
                }

                PaddleCustomerResponse? result =
                    await response.Content
                        .ReadFromJsonAsync<
                            PaddleCustomerResponse>(
                                cancellationToken:
                                    cancellationToken);

                PaddleCustomerData? customer =
                    result?.Data;

                if (customer == null ||
                    string.IsNullOrWhiteSpace(
                        customer.Id) ||
                    string.IsNullOrWhiteSpace(
                        customer.Email))
                {
                    return CustomerError(
                        "PADDLE_INVALID_CUSTOMER",
                        "Paddle did not return valid customer data.");
                }

                if (!string.Equals(
                        customer.Id,
                        customerId,
                        StringComparison.Ordinal))
                {
                    return CustomerError(
                        "PADDLE_CUSTOMER_MISMATCH",
                        "The Paddle customer does not match the transaction.");
                }

                return new PaymentCustomerResult
                {
                    Success =
                        true,

                    CustomerId =
                        customer.Id,

                    Email =
                        customer.Email,

                    ErrorCode =
                        string.Empty,

                    Message =
                        string.Empty
                };
            }
            catch (OperationCanceledException)
                when (!cancellationToken
                    .IsCancellationRequested)
            {
                return CustomerError(
                    "PADDLE_TIMEOUT",
                    "The Paddle customer request timed out.");
            }
            catch (HttpRequestException)
            {
                return CustomerError(
                    "PADDLE_NETWORK_ERROR",
                    "The Paddle API could not be reached.");
            }
            catch
            {
                return CustomerError(
                    "PADDLE_CUSTOMER_ERROR",
                    "An unexpected Paddle customer error occurred.");
            }
        }

        private void AddAuthorization(
            HttpRequestMessage request)
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    _options.ApiKey);
        }

        private bool IsValidBaseUrl()
        {
            return Uri.TryCreate(
                _options.BaseUrl,
                UriKind.Absolute,
                out Uri? baseUri) &&
                baseUri.Scheme ==
                    Uri.UriSchemeHttps;
        }

        private string GetPriceId(
            LicensePlan plan)
        {
            return plan switch
            {
                LicensePlan.OneMonth =>
                    _options.OneMonthPriceId,

                _ =>
                    string.Empty
            };
        }

        private static string GetCustomDataValue(
            Dictionary<string, string>?
                customData,
            string key)
        {
            if (customData == null)
            {
                return string.Empty;
            }

            return customData.TryGetValue(
                key,
                out string? value)
                    ? value ?? string.Empty
                    : string.Empty;
        }

        private static PaymentCheckoutResult
            CheckoutError(
                string errorCode,
                string message)
        {
            return new PaymentCheckoutResult
            {
                Success =
                    false,

                CheckoutUrl =
                    string.Empty,

                ProviderSessionId =
                    string.Empty,

                ErrorCode =
                    errorCode,

                Message =
                    message
            };
        }

        private static PaymentTransactionStatusResult
            StatusError(
                string errorCode,
                string message)
        {
            return new PaymentTransactionStatusResult
            {
                Success =
                    false,

                ErrorCode =
                    errorCode,

                Message =
                    message
            };
        }

        private static PaymentCustomerResult
            CustomerError(
                string errorCode,
                string message)
        {
            return new PaymentCustomerResult
            {
                Success =
                    false,

                CustomerId =
                    string.Empty,

                Email =
                    string.Empty,

                ErrorCode =
                    errorCode,

                Message =
                    message
            };
        }

        private sealed class
            PaddleTransactionResponse
        {
            [JsonPropertyName("data")]
            public PaddleTransactionData?
                Data
            {
                get;
                init;
            }
        }

        private sealed class
            PaddleTransactionData
        {
            [JsonPropertyName("id")]
            public string Id
            {
                get;
                init;
            } = string.Empty;

            [JsonPropertyName("checkout")]
            public PaddleCheckoutData?
                Checkout
            {
                get;
                init;
            }
        }

        private sealed class
            PaddleCheckoutData
        {
            [JsonPropertyName("url")]
            public string Url
            {
                get;
                init;
            } = string.Empty;
        }

        private sealed class
            PaddleTransactionStatusResponse
        {
            [JsonPropertyName("data")]
            public PaddleTransactionStatusData?
                Data
            {
                get;
                init;
            }
        }

        private sealed class
            PaddleTransactionStatusData
        {
            [JsonPropertyName("id")]
            public string Id
            {
                get;
                init;
            } = string.Empty;

            [JsonPropertyName("status")]
            public string Status
            {
                get;
                init;
            } = string.Empty;

            [JsonPropertyName("customer_id")]
            public string CustomerId
            {
                get;
                init;
            } = string.Empty;

            [JsonPropertyName("custom_data")]
            public Dictionary<string, string>?
                CustomData
            {
                get;
                init;
            }

            [JsonPropertyName("items")]
            public List<PaddleTransactionItem>?
                Items
            {
                get;
                init;
            }
        }

        private sealed class
            PaddleTransactionItem
        {
            [JsonPropertyName("price")]
            public PaddleTransactionPrice?
                Price
            {
                get;
                init;
            }
        }

        private sealed class
            PaddleTransactionPrice
        {
            [JsonPropertyName("id")]
            public string Id
            {
                get;
                init;
            } = string.Empty;
        }

        private sealed class
            PaddleCustomerListResponse
        {
            [JsonPropertyName("data")]
            public List<PaddleCustomerData>?
                Data
            {
                get;
                init;
            }
        }

        private sealed class
            PaddleCustomerResponse
        {
            [JsonPropertyName("data")]
            public PaddleCustomerData?
                Data
            {
                get;
                init;
            }
        }

        private sealed class
            PaddleCustomerData
        {
            [JsonPropertyName("id")]
            public string Id
            {
                get;
                init;
            } = string.Empty;

            [JsonPropertyName("email")]
            public string Email
            {
                get;
                init;
            } = string.Empty;
        }
    }
}