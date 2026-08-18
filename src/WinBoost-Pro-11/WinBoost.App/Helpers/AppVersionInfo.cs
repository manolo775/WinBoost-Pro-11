using System;
using System.Reflection;

namespace WinBoost.App.Helpers
{
    public static class AppVersionInfo
    {
        private static readonly Assembly AppAssembly =
            Assembly.GetEntryAssembly()
            ?? typeof(AppVersionInfo).Assembly;

        public static string InformationalVersion
        {
            get
            {
                string? version =
                    AppAssembly
                        .GetCustomAttribute<
                            AssemblyInformationalVersionAttribute>()
                        ?.InformationalVersion;

                return RemoveBuildMetadata(
                    version
                    ?? AppAssembly
                        .GetName()
                        .Version
                        ?.ToString()
                    ?? "0.0.0");
            }
        }

        public static string FileVersion
        {
            get
            {
                return AppAssembly
                    .GetCustomAttribute<
                        AssemblyFileVersionAttribute>()
                    ?.Version
                    ?? "0.0.0.0";
            }
        }

        public static string DisplayVersion
        {
            get
            {
                string version =
                    InformationalVersion;

                string coreVersion =
                    version.Split('-')[0];

                string[] parts =
                    coreVersion.Split('.');

                string shortVersion =
                    parts.Length >= 2
                        ? $"{parts[0]}.{parts[1]}"
                        : coreVersion;

                bool isPreview =
                    version.Contains(
                        "-preview",
                        StringComparison.OrdinalIgnoreCase);

                return isPreview
                    ? $"v{shortVersion} Preview"
                    : $"v{shortVersion}";
            }
        }

        public static bool IsPreview =>
            InformationalVersion.Contains(
                "-preview",
                StringComparison.OrdinalIgnoreCase);

        private static string RemoveBuildMetadata(
            string version)
        {
            int metadataIndex =
                version.IndexOf('+');

            return metadataIndex >= 0
                ? version[..metadataIndex]
                : version;
        }
    }
}