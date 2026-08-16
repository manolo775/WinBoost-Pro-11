using System.Windows;
using WinBoost.App.Localization;
using WinBoost.App.Services.Navigation;

namespace WinBoost.App.Helpers
{
    public static class AdministratorRequirementHelper
    {
        public static bool EnsureAdministrator()
        {
            if (ApplicationElevationHelper
                .IsRunningAsAdministrator())
            {
                return true;
            }

            bool goToSettings =
                NativeConfirmationDialog.Ask(
                    Application.Current.MainWindow,
                    LocalizationHelper.Get(
                        "AdministratorRequiredTitle"),
                    LocalizationHelper.Get(
                        "AdministratorRequiredMessage"),
                    LocalizationHelper.Get(
                        "CommonYes"),
                    LocalizationHelper.Get(
                        "CommonNo"));

            if (goToSettings)
            {
                AppNavigationService
                    .NavigateToSettings();
            }

            return false;
        }
    }
}