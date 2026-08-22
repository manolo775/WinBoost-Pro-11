using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text;
using System.IO.Compression;
using WinBoost.Licensing.Server.Configuration;
using WinBoost.Licensing.Server.Data;
using WinBoost.Licensing.Server.Models;
using WinBoost.Licensing.Server.Services;

namespace WinBoost.Licensing.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder =
                WebApplication.CreateBuilder(args);

            string licensingConnectionString =
                builder.Configuration
                    .GetConnectionString(
                        "LicensingDatabase")
                ?? throw new InvalidOperationException(
                    "Licensing database connection string is missing.");

            builder.Services
                .AddDbContext<LicensingDbContext>(
                    options =>
                        options.UseSqlite(
                            licensingConnectionString));

            builder.Services
                .Configure<LicenseOffersOptions>(
                    builder.Configuration.GetSection(
                        LicenseOffersOptions.SectionName));

            builder.Services
                .Configure<PaddleOptions>(
                    builder.Configuration.GetSection(
                        PaddleOptions.SectionName));

            builder.Services
                .Configure<LicenseSigningOptions>(
                    builder.Configuration.GetSection(
                        LicenseSigningOptions.SectionName));

            builder.Services
                .Configure<PaddleWebhookOptions>(
                    builder.Configuration.GetSection(
                        PaddleWebhookOptions.SectionName));

            builder.Services
                 .Configure<UpdateManifestOptions>(
                  builder.Configuration.GetSection(
                  UpdateManifestOptions.SectionName));

            builder.Services
                .AddSingleton<LicenseOffersService>();

            builder.Services
                .AddScoped<PurchaseRepository>();

            builder.Services
                .AddScoped<LicenseRepository>();

            builder.Services
                .AddScoped<TrialRepository>();

            builder.Services
                 .AddScoped<TrialActivationService>();

            builder.Services
                .AddHttpClient<
                    IPaymentProvider,
                    PaddlePaymentProvider>();

            builder.Services
                .AddScoped<PurchaseSessionService>();

            builder.Services
                .AddScoped<LicenseActivationCheckService>();

            builder.Services
                .AddScoped<LicenseSigningService>();

            builder.Services
                .AddScoped<LicenseIssuerService>();

            builder.Services
               .AddScoped<LicenseRevocationCheckService>();

            builder.Services
                .AddSingleton<
                    PaddleWebhookSignatureVerifier>();

            builder.Services
                .AddSingleton<
                    PaddleWebhookEventParser>();

            builder.Services
                  .AddScoped<
                   PaddleWebhookProcessingService>();

            // Add services to the container.
            builder.Services.AddControllers();

            // Learn more about configuring OpenAPI.
            builder.Services.AddOpenApi();

            var app =
                builder.Build();

            // ======================================
            // PRODUCTION UPDATE MANIFEST SAFETY
            // ======================================

            if (!app.Environment.IsDevelopment())
            {
                UpdateManifestOptions manifest =
                    app.Services
                        .GetRequiredService<
                            IOptions<UpdateManifestOptions>>()
                        .Value;

                if (string.IsNullOrWhiteSpace(
                        manifest.Version))
                {
                    throw new InvalidOperationException(
                        "The production update manifest version is not configured.");
                }

                if (string.IsNullOrWhiteSpace(
                        manifest.DownloadUrl))
                {
                    throw new InvalidOperationException(
                        "The production update download URL is not configured.");
                }

                if (!Uri.TryCreate(
                        manifest.DownloadUrl,
                        UriKind.Absolute,
                        out Uri? downloadUri))
                {
                    throw new InvalidOperationException(
                        "The production update download URL is invalid.");
                }

                if (!string.Equals(
                        downloadUri.Scheme,
                        Uri.UriSchemeHttps,
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        "The production update download URL must use HTTPS.");
                }

                if (downloadUri.IsLoopback ||
                    string.Equals(
                        downloadUri.Host,
                        "localhost",
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        "The production update download URL cannot use localhost or a loopback address.");
                }
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            // ======================================
            // LICENSING OFFERS
            // ======================================

            if (app.Environment.IsDevelopment())
            {
                app.MapGet(
                    "/api/update/package/preview-3",
                    () =>
                    {
                        string packagePath =
                            Path.Combine(
                                Path.GetTempPath(),
                                "WinBoost",
                                "WinBoost-1.0.0-preview.3.zip");

                        if (!File.Exists(packagePath))
                        {
                            return Results.NotFound(
                                "WinBoost preview update package was not found.");
                        }

                        return Results.File(
                            packagePath,
                            "application/zip",
                            "WinBoost-1.0.0-preview.3.zip");
                    });
            }
            // ======================================
            // PURCHASE SESSION
            // ======================================

            app.MapPost(
                "/api/licensing/purchase-session",
                async (
                    PurchaseSessionRequest request,
                    PurchaseSessionService
                        purchaseSessionService,
                    CancellationToken
                        cancellationToken) =>
                {
                    PurchaseSessionResponse response =
                        await purchaseSessionService
                            .CreatePurchaseSessionAsync(
                                request,
                                cancellationToken);

                    if (response.Success)
                    {
                        return Results.Ok(
                            response);
                    }

                    return Results.BadRequest(
                        response);
                });

            // ======================================
            // PADDLE CHECKOUT PAGE
            // ======================================

            app.MapGet(
                "/checkout",
                (
                    IOptions<PaddleOptions>
                        paddleOptions) =>
                {
                    string clientSideToken =
                        paddleOptions.Value
                            .ClientSideToken;

                    if (string.IsNullOrWhiteSpace(
                            clientSideToken))
                    {
                        return Results.Problem(
                            "Paddle client-side token is not configured.");
                    }

                    string encodedToken =
                        System.Text.Encodings.Web
                            .JavaScriptEncoder
                            .Default
                            .Encode(
                                clientSideToken);

                    string html =
                        $$"""
                        <!DOCTYPE html>
                        <html lang="en">

                        <head>
                            <meta charset="utf-8" />

                            <meta
                                name="viewport"
                                content="width=device-width, initial-scale=1" />

                            <title>
                                WinBoost Pro 11 Checkout
                            </title>

                            <script
                                src="https://cdn.paddle.com/paddle/v2/paddle.js">
                            </script>
                        </head>

                        <body>
                            <h2>
                                WinBoost Pro 11
                            </h2>

                            <p>
                                Secure checkout is loading...
                            </p>

                            <script>
                                Paddle.Environment.set(
                                    "sandbox"
                                );

                                Paddle.Initialize({
                                    token: "{{encodedToken}}"
                                });
                            </script>
                        </body>

                        </html>
                        """;

                    return Results.Content(
                        html,
                        "text/html; charset=utf-8");
                });

            // ======================================
            // CHECK LICENSE ACTIVATION
            // ======================================

            app.MapPost(
                "/api/licensing/check-activation",
                async (
                    LicenseActivationCheckRequest
                        request,
                    LicenseActivationCheckService
                        activationCheckService,
                    CancellationToken
                        cancellationToken) =>
                {
                    LicenseActivationCheckResponse
                        response =
                            await activationCheckService
                                .VerifyPaymentAsync(
                                    request,
                                    cancellationToken);

                    return Results.Ok(
                        response);
                });

            // ======================================
            // CHECK LICENSE REVOCATION
            // ======================================

            app.MapPost(
                "/api/licensing/check-revocation",
                async (
                    LicenseRevocationCheckRequest request,
                    LicenseRevocationCheckService
                        revocationCheckService,
                    CancellationToken
                        cancellationToken) =>
                {
                    LicenseRevocationCheckResponse response =
                        await revocationCheckService
                            .CheckAsync(
                                request,
                                cancellationToken);

                    if (!response.Success)
                    {
                        return Results.BadRequest(
                            response);
                    }

                    return Results.Ok(
                        response);
                });

            // ======================================
            // TRIAL ACTIVATION
            // ======================================

            app.MapPost(
                "/api/licensing/trial-activation",
                async (
                    TrialActivationRequest request,
                    TrialActivationService
                        trialActivationService,
                    CancellationToken
                        cancellationToken) =>
                {
                    TrialActivationResponse response =
                        await trialActivationService
                            .ActivateAsync(
                                request,
                                cancellationToken);

                    if (!response.Success)
                    {
                        return Results.BadRequest(
                            response);
                    }

                    return Results.Ok(
                        response);
                });

            // ======================================
            // PADDLE WEBHOOK
            // ======================================

            app.MapPost(
     "/api/paddle/webhook",
     async (
         HttpRequest request,
         PaddleWebhookSignatureVerifier
             signatureVerifier,
         PaddleWebhookEventParser
             eventParser,
         PaddleWebhookProcessingService
             processingService,
         CancellationToken
             cancellationToken) =>
     {
         // ----------------------------------
         // Paddle signature header
         // ----------------------------------

         if (!request.Headers.TryGetValue(
                 "Paddle-Signature",
                 out var signatureValues))
         {
             return Results.Unauthorized();
         }

         // ----------------------------------
         // Read exact webhook body
         // ----------------------------------

         using var reader =
             new StreamReader(
                 request.Body,
                 Encoding.UTF8);

         string rawBody =
             await reader.ReadToEndAsync();

         string signatureHeader =
             signatureValues.ToString();

         // ----------------------------------
         // Verify Paddle signature
         // ----------------------------------

         bool isValid =
             signatureVerifier.Verify(
                 rawBody,
                 signatureHeader,
                 out string failureReason);

         if (!isValid)
         {
             return Results.Json(
                 new
                 {
                     received = true,
                     signatureValid = false,
                     reason = failureReason
                 },
                 statusCode:
                     StatusCodes
                         .Status401Unauthorized);
         }

         // ----------------------------------
         // Parse transaction.completed
         // ----------------------------------

         bool eventValid =
             eventParser
                 .TryParseTransactionCompleted(
                     rawBody,
                     out string transactionId,
                     out string eventId,
                     out string eventFailureReason);

         if (!eventValid)
         {
             return Results.Json(
                 new
                 {
                     received = true,
                     signatureValid = true,
                     eventValid = false,
                     reason =
                         eventFailureReason
                 },
                 statusCode:
                     StatusCodes
                         .Status400BadRequest);
         }

         // ----------------------------------
         // Process completed transaction
         // ----------------------------------

         PaddleWebhookProcessingResult
             processingResult =
                 await processingService
                     .ProcessTransactionCompletedAsync(
                         transactionId,
                         cancellationToken);

         if (!processingResult.Success)
         {
             return Results.Json(
                 new
                 {
                     received = true,
                     signatureValid = true,
                     eventValid = true,
                     processed = false,
                     eventId,
                     transactionId,
                     errorCode =
                         processingResult.ErrorCode,
                     message =
                         processingResult.Message
                 },
                 statusCode:
                     StatusCodes
                         .Status422UnprocessableEntity);
         }

         // ----------------------------------
         // Transaction processed successfully
         // ----------------------------------

         return Results.Ok(
             new
             {
                 received = true,
                 signatureValid = true,
                 eventValid = true,
                 processed = true,
                 eventId,
                 transactionId
             });
     });

            // ======================================
            // WINBOOST APPLICATION UPDATE MANIFEST
            // ======================================

            app.MapGet(
                "/api/update/manifest",
                (
                    IOptions<UpdateManifestOptions>
                        updateManifestOptions) =>
                {
                    UpdateManifestOptions manifest =
                        updateManifestOptions.Value;

                    if (string.IsNullOrWhiteSpace(
                            manifest.Version))
                    {
                        return Results.Problem(
                            "Update manifest version is not configured.");
                    }

                    return Results.Ok(
                        new
                        {
                            version =
                                manifest.Version,

                            channel =
                                manifest.Channel,

                            downloadUrl =
                                manifest.DownloadUrl,

                            sha256 =
                                manifest.Sha256,

                            releaseNotes =
                                manifest.ReleaseNotes
                        });
                });

            app.Run();
        }
    }
}