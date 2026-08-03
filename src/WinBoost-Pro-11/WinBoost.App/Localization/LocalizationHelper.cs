using System;
using System.Windows;

namespace WinBoost.App.Localization
{
    public static class LocalizationHelper
    {
        public static string Get(
            string resourceKey)
        {
            if (string.IsNullOrWhiteSpace(resourceKey))
            {
                return string.Empty;
            }

            object? resource =
                Application.Current.TryFindResource(
                    resourceKey);

            return resource?.ToString()
                ?? resourceKey;
        }

        public static string Format(
            string resourceKey,
            params object[] arguments)
        {
            string format =
                Get(resourceKey);

            try
            {
                return string.Format(
                    format,
                    arguments);
            }
            catch (FormatException)
            {
                return format;
            }
        }
    }
}