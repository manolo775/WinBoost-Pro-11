using System.Security.Principal;

namespace WinBoost.App.Helpers
{
    public static class AdministratorHelper
    {
        public static bool IsRunningAsAdministrator()
        {
            using WindowsIdentity identity =
                WindowsIdentity.GetCurrent();

            WindowsPrincipal principal =
                new WindowsPrincipal(identity);

            return principal.IsInRole(
                WindowsBuiltInRole.Administrator);
        }
    }
}