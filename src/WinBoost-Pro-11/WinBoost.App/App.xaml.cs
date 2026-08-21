using System.Threading.Tasks;
using System.Windows;
using WinBoost.App.Models;
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

            _ = InitializeLicensingOnStartupAsync();
        }

        private static async Task
            InitializeLicensingOnStartupAsync()
        {
            try
            {
                LicenseService licenseService =
                    LicenseService.Instance;

                // An existing active paid license or Trial
                // remains the local source of truth.
                //
                // We only perform the server-side
                // revocation check here.
                if (licenseService.IsActive)
                {
                    var revocationService =
                        new LicenseRevocationService();

                    await revocationService
                        .CheckCurrentLicenseAsync();

                    return;
                }

                // Expired and Invalid licenses must never
                // receive a new automatic Trial.
                //
                // Automatic Trial activation is attempted
                // only when WinBoost has no license at all.
                if (licenseService.Status !=
                    LicenseStatus.Unlicensed)
                {
                    return;
                }

                var trialActivationService =
                    new TrialActivationService();

                await trialActivationService
                    .ActivateTrialAsync();
            }
            catch
            {
                // Licensing initialization must never
                // prevent WinBoost from starting.
                //
                // If the server or network is unavailable,
                // WinBoost remains in its current local
                // licensing state.
            }
        }
    }
}