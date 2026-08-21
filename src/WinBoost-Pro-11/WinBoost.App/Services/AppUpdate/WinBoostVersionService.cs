using System;
using System.Reflection;

namespace WinBoost.App.Services.AppUpdate
{
    public sealed class WinBoostVersionService
    {
        public string GetCurrentVersion()
        {
            Assembly assembly =
                typeof(global::WinBoost.App.App).Assembly;

            AssemblyInformationalVersionAttribute?
                informationalVersionAttribute =
                    assembly.GetCustomAttribute<
                        AssemblyInformationalVersionAttribute>();

            string? informationalVersion =
                informationalVersionAttribute?
                    .InformationalVersion;

            if (!string.IsNullOrWhiteSpace(
                    informationalVersion))
            {
                return informationalVersion.Trim();
            }

            Version? assemblyVersion =
                assembly.GetName().Version;

            return assemblyVersion?.ToString()
                ?? "0.0.0";
        }
    }
}