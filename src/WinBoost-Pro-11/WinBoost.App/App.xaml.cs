using System.Threading.Tasks;
using System.Windows;
using WinBoost.App.Services.Licensing;

namespace WinBoost.App
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(
            StartupEventArgs e)
        {
            base.OnStartup(e);

            _ = CheckLicenseRevocationOnStartupAsync();
        }

        private static async Task
            CheckLicenseRevocationOnStartupAsync()
        {
            try
            {
                var revocationService =
                    new LicenseRevocationService();

                await revocationService
                    .CheckCurrentLicenseAsync();
            }
            catch
            {
                // Revocation checking must never prevent
                // WinBoost from starting.
                //
                // If the server or network is unavailable,
                // the locally signed license remains valid.
            }
        }
    }
}