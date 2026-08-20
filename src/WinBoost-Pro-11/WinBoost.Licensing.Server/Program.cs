using WinBoost.Licensing.Server.Configuration;
using WinBoost.Licensing.Server.Services;
using WinBoost.Licensing.Server.Models;
using Microsoft.EntityFrameworkCore;
using WinBoost.Licensing.Server.Data;

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

            builder.Services.AddSingleton<LicenseOffersService>();

            builder.Services.AddScoped<PurchaseRepository>();

            builder.Services.AddScoped<
               IPaymentProvider,
              UnconfiguredPaymentProvider>();

            builder.Services.AddScoped<PurchaseSessionService>();

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

            app.Run();

     
        }
    }
}
