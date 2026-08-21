using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using WinBoost.App.Models;

namespace WinBoost.App.Services.AppUpdate
{
    public sealed class WinBoostUpdateDownloadService
    {
        private readonly HttpClient
            _httpClient;

        public WinBoostUpdateDownloadService()
        {
            _httpClient =
                new HttpClient
                {
                    Timeout =
                        TimeSpan.FromMinutes(10)
                };
        }

        public async Task<WinBoostUpdateDownloadResult>
            DownloadAndVerifyAsync(
                string downloadUrl,
                string expectedSha256,
                CancellationToken cancellationToken =
                    default)
        {
            if (!Uri.TryCreate(
                    downloadUrl,
                    UriKind.Absolute,
                    out Uri? downloadUri) ||
                downloadUri.Scheme !=
                    Uri.UriSchemeHttps)
            {
                return Fail(
                    "INVALID_DOWNLOAD_URL",
                    "The update download URL is invalid.");
            }

            string normalizedExpectedHash =
                NormalizeHash(
                    expectedSha256);

            if (normalizedExpectedHash.Length != 64)
            {
                return Fail(
                    "INVALID_SHA256",
                    "The expected SHA-256 value is invalid.");
            }

            string updateDirectory =
                Path.Combine(
                    Path.GetTempPath(),
                    "WinBoost",
                    "Updates");

            Directory.CreateDirectory(
                updateDirectory);

            string fileName =
                Path.GetFileName(
                    downloadUri.LocalPath);

            if (string.IsNullOrWhiteSpace(
                    fileName))
            {
                fileName =
                    "winboost-update.bin";
            }

            string destinationPath =
                Path.Combine(
                    updateDirectory,
                    fileName);

            try
            {
                using HttpResponseMessage response =
                    await _httpClient.GetAsync(
                        downloadUri,
                        HttpCompletionOption
                            .ResponseHeadersRead,
                        cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return Fail(
                        "DOWNLOAD_FAILED",
                        $"Update server returned HTTP " +
                        $"{(int)response.StatusCode}.");
                }

                await using Stream sourceStream =
    await response.Content
        .ReadAsStreamAsync(
            cancellationToken);

                await using (
                    FileStream destinationStream =
                        new FileStream(
                            destinationPath,
                            FileMode.Create,
                            FileAccess.Write,
                            FileShare.None,
                            bufferSize: 81920,
                            useAsync: true))
                {
                    await sourceStream.CopyToAsync(
                        destinationStream,
                        cancellationToken);

                    await destinationStream
                        .FlushAsync(
                            cancellationToken);
                }

                string actualSha256 =
                    await CalculateSha256Async(
                        destinationPath,
                        cancellationToken);

                if (!string.Equals(
                        normalizedExpectedHash,
                        actualSha256,
                        StringComparison.OrdinalIgnoreCase))
                {
                    TryDelete(
                        destinationPath);

                    return new WinBoostUpdateDownloadResult
                    {
                        Success =
                            false,

                        ExpectedSha256 =
                            normalizedExpectedHash,

                        ActualSha256 =
                            actualSha256,

                        ErrorCode =
                            "SHA256_MISMATCH",

                        Details =
                            "The downloaded update failed integrity verification."
                    };
                }

                return new WinBoostUpdateDownloadResult
                {
                    Success =
                        true,

                    FilePath =
                        destinationPath,

                    ExpectedSha256 =
                        normalizedExpectedHash,

                    ActualSha256 =
                        actualSha256,

                    Details =
                        "The update was downloaded and verified successfully."
                };
            }
            catch (OperationCanceledException)
            {
                TryDelete(
                    destinationPath);

                throw;
            }
            catch (HttpRequestException ex)
            {
                TryDelete(
                    destinationPath);

                return Fail(
                    "NETWORK_ERROR",
                    ex.Message);
            }
            catch (Exception ex)
            {
                TryDelete(
                    destinationPath);

                return Fail(
                    "DOWNLOAD_ERROR",
                    ex.Message);
            }
        }

        private static async Task<string>
            CalculateSha256Async(
                string filePath,
                CancellationToken cancellationToken)
        {
            await using FileStream stream =
                new FileStream(
                    filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    bufferSize: 81920,
                    useAsync: true);

            using SHA256 sha256 =
                SHA256.Create();

            byte[] hash =
                await sha256.ComputeHashAsync(
                    stream,
                    cancellationToken);

            return Convert
                .ToHexString(hash)
                .ToLowerInvariant();
        }

        private static string NormalizeHash(
            string value)
        {
            return string.IsNullOrWhiteSpace(
                    value)
                ? string.Empty
                : value
                    .Trim()
                    .Replace(
                        " ",
                        string.Empty)
                    .ToLowerInvariant();
        }

        private static WinBoostUpdateDownloadResult
            Fail(
                string errorCode,
                string details)
        {
            return new WinBoostUpdateDownloadResult
            {
                Success =
                    false,

                ErrorCode =
                    errorCode,

                Details =
                    details
            };
        }

        private static void TryDelete(
            string filePath)
        {
            try
            {
                if (File.Exists(
                        filePath))
                {
                    File.Delete(
                        filePath);
                }
            }
            catch
            {
                // A failed cleanup must not hide
                // the original update error.
            }
        }
    }
}