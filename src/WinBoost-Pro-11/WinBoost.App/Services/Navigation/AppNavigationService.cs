using System;

namespace WinBoost.App.Services.Navigation
{
    public static class AppNavigationService
    {
        public static event Action<string>?
            NavigationRequested;

        public static string CurrentPage
        {
            get;
            private set;
        } = "Dashboard";

        public static string?
            ReturnPageAfterPrivilegeRestart
        {
            get;
            private set;
        }

        public static void SetCurrentPage(
            string page)
        {
            if (string.IsNullOrWhiteSpace(
                    page))
            {
                return;
            }

            CurrentPage = page;
        }

        public static void NavigateTo(
            string page)
        {
            if (string.IsNullOrWhiteSpace(
                    page))
            {
                return;
            }

            NavigationRequested?.Invoke(
                page);
        }

        public static void NavigateToSettings()
        {
            if (!string.Equals(
                    CurrentPage,
                    "Settings",
                    StringComparison.OrdinalIgnoreCase))
            {
                ReturnPageAfterPrivilegeRestart =
                    CurrentPage;
            }

            NavigateTo(
                "Settings");
        }

        public static void ClearReturnPage()
        {
            ReturnPageAfterPrivilegeRestart =
                null;
        }
    }
}