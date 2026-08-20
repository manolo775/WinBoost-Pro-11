using WinBoost.Licensing.Server.Configuration;
using WinBoost.Licensing.Server.Services;
using WinBoost.Licensing.Server.Models;
using Microsoft.EntityFrameworkCore;
using WinBoost.Licensing.Server.Data;
using Microsoft.Extensions.Options;
using System.Text;

namespace WinBoost.Licensing.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            string licensingConnectionString =
                 builder.Configuration
                .GetConnectionString(
                "LicensingDatabase")
                ?? throw new InvalidOperationException(
                "Licensing database connection string is missing.");

            builder.Services.AddDbContext<LicensingDbContext>(
                options =>
                    options.UseSqlite(
                        licensingConnectionString));

            builder.Services.Configure<LicenseOffersOptions>(
              builder.Configuration.GetSection(
                 LicenseOffersOptions.SectionName));

            builder.Services.Configure<PaddleOptions>(
                     builder.Configuration.GetSection(
                     PaddleOptions.SectionName));

            builder.Services.Configure<LicenseSigningOptions>(
                    builder.Configuration.GetSection(
                    LicenseSigningOptions.SectionName));

            builder.Services.Configure<PaddleWebhookOptions>(
                builder.Configuration.GetSection(
                PaddleWebhookOptions.SectionName));

            builder.Services.AddSingleton<LicenseOffersService>();

            builder.Services.AddScoped<PurchaseRepository>();

            builder.Services.AddScoped<LicenseRepository>();

            builder.Services.AddHttpClient<
                   IPaymentProvider,
                   PaddlePaymentProvider>();

            builder.Services.AddScoped<PurchaseSessionService>();

            builder.Services.AddScoped<LicenseActivationCheckService>();

            builder.Services.AddScoped<LicenseSigningService>();

            builder.Services.AddScoped<LicenseIssuerService>();

            builder.Services.AddSingleton<PaddleWebhookSignatureVerifier>();

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.MapGet(
                  "/api/licensing/offers",
                   (LicenseOffersService licenseOffersService) =>
                {
                     var response =
                      licenseOffersService.GetCurrentOffers();

                        return Results.Ok(response);
                 });

            app.MapPost(
     "/api/licensing/purchase-session",
     async (
         PurchaseSessionRequest request,
         PurchaseSessionService purchaseSessionService,
         CancellationToken cancellationToken) =>
     {
         PurchaseSessionResponse response =
             await purchaseSessionService
                 .CreatePurchaseSessionAsync(
                     request,
                     cancellationToken);

         if (response.Success)
         {
             return Results.Ok(response);
         }

         return Results.BadRequest(response);
     });

            app.MapGet(
    "/checkout",
    (IOptions<PaddleOptions> paddleOptions) =>
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
                <meta name="viewport"
                      content="width=device-width, initial-scale=1" />

                <title>WinBoost Pro 11 Checkout</title>

                <script src="https://cdn.paddle.com/paddle/v2/paddle.js"></script>
            </head>

            <body>
                <h2>WinBoost Pro 11</h2>
                <p>Secure checkout is loading...</p>

                <script>
                    Paddle.Environment.set("sandbox");

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

            app.MapPost(
    "/api/licensing/check-activation",
    async (
        LicenseActivationCheckRequest request,
        LicenseActivationCheckService activationCheckService,
        CancellationToken cancellationToken) =>
    {
        LicenseActivationCheckResponse response =
            await activationCheckService
                .VerifyPaymentAsync(
                    request,
                    cancellationToken);

        return Results.Ok(response);
    });

            app.MapPost(
    "/api/paddle/webhook",
    async (
        HttpRequest request,
        PaddleWebhookSignatureVerifier signatureVerifier) =>
    {
        if (!request.Headers.TryGetValue(
                "Paddle-Signature",
                out var signatureValues))
        {
            return Results.Unauthorized();
        }

        using var reader =
            new StreamReader(
                request.Body,
                Encoding.UTF8);

        string rawBody =
            await reader.ReadToEndAsync();

        string signatureHeader =
            signatureValues.ToString();

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
                    StatusCodes.Status401Unauthorized);
        }

        return Results.Ok(
     new
     {
         received = true,
         signatureValid = true
     });
    });

            app.Run();

     
        }
    }
}
