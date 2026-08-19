using WinBoost.Licensing.Server.Configuration;
using WinBoost.Licensing.Server.Services;
using WinBoost.Licensing.Server.Models;

namespace WinBoost.Licensing.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.Configure<LicenseOffersOptions>(
              builder.Configuration.GetSection(
                 LicenseOffersOptions.SectionName));

            builder.Services.AddSingleton<LicenseOffersService>();

            builder.Services.AddSingleton<PurchaseSessionService>();

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
    (
        PurchaseSessionRequest request,
        PurchaseSessionService purchaseSessionService) =>
    {
        PurchaseSessionResponse response =
            purchaseSessionService
                .CreatePurchaseSession(request);

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
