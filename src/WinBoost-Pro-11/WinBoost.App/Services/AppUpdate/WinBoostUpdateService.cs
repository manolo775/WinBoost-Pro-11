using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WinBoost.App.Models;

namespace WinBoost.App.Services.AppUpdate
{
    public sealed class WinBoostUpdateService
    {
        private readonly HttpClient _httpClient;
        private readonly WinBoostVersionService
            _versionService;

        public WinBoostUpdateService()
        {
            _httpClient =
                new HttpClient();

            _versionService =
                new WinBoostVersionService();
        }

        public async Task<WinBoostUpdateCheckResult>
            CheckForUpdatesAsync(
                string manifestUrl,
                CancellationToken cancellationToken =
                    default)
        {
            string currentVersion =
                _versionService.GetCurrentVersion();

            if (string.IsNullOrWhiteSpace(
                    manifestUrl))
            {
                return new WinBoostUpdateCheckResult
                {
                    Status =
                        WinBoostUpdateStatus.Unavailable,

                    CurrentVersion =
                        currentVersion,

                    Details =
                        "Update manifest URL is unavailable."
                };
            }

            try
            {
                using HttpResponseMessage response =
                    await _httpClient.GetAsync(
                        manifestUrl,
                        cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return new WinBoostUpdateCheckResult
                    {
                        Status =
                            WinBoostUpdateStatus.Unavailable,

                        CurrentVersion =
                            currentVersion,

                        Details =
                            $"Update server returned HTTP " +
                            $"{(int)response.StatusCode}."
                    };
                }

                string json =
                    await response.Content
                        .ReadAsStringAsync(
                            cancellationToken);

                WinBoostUpdateManifest? manifest =
                    JsonSerializer.Deserialize<
                        WinBoostUpdateManifest>(
                            json,
                            new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive =
                                    true
                            });

                if (manifest == null ||
                    string.IsNullOrWhiteSpace(
                        manifest.Version))
                {
                    return new WinBoostUpdateCheckResult
                    {
                        Status =
                            WinBoostUpdateStatus.Failed,

                        CurrentVersion =
                            currentVersion,

                        Details =
                            "The update manifest is invalid."
                    };
                }

                int comparison =
                    SemanticVersionComparer.Compare(
                        manifest.Version,
                        currentVersion);

                WinBoostUpdateStatus status =
                    comparison > 0
                        ? WinBoostUpdateStatus
                            .UpdateAvailable
                        : WinBoostUpdateStatus
                            .UpToDate;

                return new WinBoostUpdateCheckResult
                {
                    Status = status,

                    CurrentVersion =
                        currentVersion,

                    AvailableVersion =
                        manifest.Version,

                    Channel =
                        manifest.Channel,

                    DownloadUrl =
                        manifest.DownloadUrl,

                    Sha256 =
                        manifest.Sha256,

                    ReleaseNotes =
                        manifest.ReleaseNotes,

                    Details =
                        status ==
                        WinBoostUpdateStatus.UpdateAvailable
                            ? "A new WinBoost version is available."
                            : "WinBoost is up to date."
                };
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (HttpRequestException ex)
            {
                return new WinBoostUpdateCheckResult
                {
                    Status =
                        WinBoostUpdateStatus.Unavailable,

                    CurrentVersion =
                        currentVersion,

                    Details =
                        ex.Message
                };
            }
            catch (Exception ex)
            {
                return new WinBoostUpdateCheckResult
                {
                    Status =
                        WinBoostUpdateStatus.Failed,

                    CurrentVersion =
                        currentVersion,

                    Details =
                        ex.Message
                };
            }
        }
    }
}