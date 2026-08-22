using System;

namespace WinBoost.App.Services.AppUpdate
{
    public static class WinBoostUpdateConfiguration
    {
        private const string DevelopmentManifestEndpoint =
            "https://localhost:7160/api/update/manifest";

        private const string ProductionManifestEndpoint =
            "";

        public static string ManifestEndpoint
        {
            get
            {
#if DEBUG
                return DevelopmentManifestEndpoint;
#else
                if (string.IsNullOrWhiteSpace(
                        ProductionManifestEndpoint))
                {
                    throw new InvalidOperationException(
                        "The production WinBoost update manifest endpoint has not been configured.");
                }

                if (!Uri.TryCreate(
                        ProductionManifestEndpoint,
                        UriKind.Absolute,
                        out Uri? endpointUri) ||
                    !string.Equals(
                        endpointUri.Scheme,
                        Uri.UriSchemeHttps,
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        "The production WinBoost update manifest endpoint must be a valid HTTPS URL.");
                }

                return ProductionManifestEndpoint;
#endif
            }
        }
    }
}