using System;
using System.Linq;

namespace WinBoost.App.Services.WindowsUpdate
{
    public enum WindowsUpdateAdvisorType
    {
        Security,
        System,
        Driver,
        Optional,
        Other
    }

    public sealed class WindowsUpdateAdvisorResult
    {
        public WindowsUpdateAdvisorType Type
        {
            get;
            init;
        }

        public string Severity
        {
            get;
            init;
        } = string.Empty;

        public bool IsRecommended
        {
            get;
            init;
        }
    }

    public sealed class WindowsUpdateAdvisor
    {
        public WindowsUpdateAdvisorResult Analyze(
            WindowsUpdateAvailableInfo update)
        {
            if (update == null)
            {
                throw new ArgumentNullException(
                    nameof(update));
            }

            string title =
                update.Title ?? string.Empty;

            string description =
                update.Description ?? string.Empty;

            bool hasSecurityCategory =
                update.Categories.Any(
                    category =>
                        category.Contains(
                            "Security",
                            StringComparison.OrdinalIgnoreCase));

            bool hasDriverCategory =
                update.Categories.Any(
                    category =>
                        category.Contains(
                            "Driver",
                            StringComparison.OrdinalIgnoreCase));

            bool isOptional =
                title.Contains(
                    "Preview",
                    StringComparison.OrdinalIgnoreCase) ||
                description.Contains(
                    "optional",
                    StringComparison.OrdinalIgnoreCase);

            if (hasSecurityCategory ||
                !string.IsNullOrWhiteSpace(
                    update.MsrcSeverity))
            {
                return new WindowsUpdateAdvisorResult
                {
                    Type =
                        WindowsUpdateAdvisorType.Security,

                    Severity =
                        update.MsrcSeverity,

                    IsRecommended =
                        true
                };
            }

            if (hasDriverCategory)
            {
                return new WindowsUpdateAdvisorResult
                {
                    Type =
                        WindowsUpdateAdvisorType.Driver,

                    Severity =
                        string.Empty,

                    IsRecommended =
                        false
                };
            }

            if (isOptional)
            {
                return new WindowsUpdateAdvisorResult
                {
                    Type =
                        WindowsUpdateAdvisorType.Optional,

                    Severity =
                        string.Empty,

                    IsRecommended =
                        false
                };
            }

            if (title.Contains(
                    "Windows",
                    StringComparison.OrdinalIgnoreCase) ||
                title.Contains(
                    ".NET",
                    StringComparison.OrdinalIgnoreCase))
            {
                return new WindowsUpdateAdvisorResult
                {
                    Type =
                        WindowsUpdateAdvisorType.System,

                    Severity =
                        string.Empty,

                    IsRecommended =
                        true
                };
            }

            return new WindowsUpdateAdvisorResult
            {
                Type =
                    WindowsUpdateAdvisorType.Other,

                Severity =
                    string.Empty,

                IsRecommended =
                    true
            };
        }
    }
}