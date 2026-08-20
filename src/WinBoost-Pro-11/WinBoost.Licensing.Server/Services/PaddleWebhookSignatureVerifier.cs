using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using WinBoost.Licensing.Server.Configuration;

namespace WinBoost.Licensing.Server.Services
{
    public sealed class PaddleWebhookSignatureVerifier
    {
        private const long TimestampToleranceSeconds =
     5;

        private readonly PaddleWebhookOptions
            _options;

        public PaddleWebhookSignatureVerifier(
            IOptions<PaddleWebhookOptions> options)
        {
            _options =
                options.Value;
        }

        public bool Verify(
            string rawBody,
            string signatureHeader,
            out string failureReason)
        {
            failureReason =
                string.Empty;

            string secretKey =
                _options.SecretKey?.Trim()
                ?? string.Empty;

            if (string.IsNullOrWhiteSpace(
                    secretKey))
            {
                failureReason =
                    "SECRET_NOT_CONFIGURED";

                return false;
            }

            if (!secretKey.StartsWith(
                    "pdl_ntfset_",
                    StringComparison.Ordinal))
            {
                failureReason =
                    "INVALID_SECRET_FORMAT";

                return false;
            }

            if (string.IsNullOrWhiteSpace(
                    rawBody))
            {
                failureReason =
                    "EMPTY_BODY";

                return false;
            }

            if (string.IsNullOrWhiteSpace(
                    signatureHeader))
            {
                failureReason =
                    "SIGNATURE_HEADER_MISSING";

                return false;
            }

            if (!TryParseSignatureHeader(
                    signatureHeader,
                    out long timestamp,
                    out List<string> signatures))
            {
                failureReason =
                    "SIGNATURE_HEADER_INVALID";

                return false;
            }

            long currentTimestamp =
                DateTimeOffset.UtcNow
                    .ToUnixTimeSeconds();

            long difference =
                Math.Abs(
                    currentTimestamp -
                    timestamp);

            if (difference >
                TimestampToleranceSeconds)
            {
                failureReason =
                    $"TIMESTAMP_OUTSIDE_TOLERANCE_{difference}";

                return false;
            }

            string signedPayload =
                string.Concat(
                    timestamp.ToString(
                        CultureInfo.InvariantCulture),
                    ":",
                    rawBody);

            byte[] expectedSignature;

            using (var hmac =
                new HMACSHA256(
                    Encoding.UTF8.GetBytes(
                        secretKey)))
            {
                expectedSignature =
                    hmac.ComputeHash(
                        Encoding.UTF8.GetBytes(
                            signedPayload));
            }

            foreach (string signature
                in signatures)
            {
                byte[] actualSignature;

                try
                {
                    actualSignature =
                        Convert.FromHexString(
                            signature);
                }
                catch (FormatException)
                {
                    continue;
                }

                if (actualSignature.Length !=
                    expectedSignature.Length)
                {
                    continue;
                }

                if (CryptographicOperations
                    .FixedTimeEquals(
                        actualSignature,
                        expectedSignature))
                {
                    failureReason =
                        "VALID";

                    return true;
                }
            }

            failureReason =
                "SIGNATURE_MISMATCH";

            return false;
        }

        private static bool
            TryParseSignatureHeader(
                string signatureHeader,
                out long timestamp,
                out List<string> signatures)
        {
            timestamp =
                0;

            signatures =
                new List<string>();

            string[] parts =
                signatureHeader.Split(
                    ';',
                    StringSplitOptions
                        .RemoveEmptyEntries |
                    StringSplitOptions
                        .TrimEntries);

            foreach (string part in parts)
            {
                int separatorIndex =
                    part.IndexOf('=');

                if (separatorIndex <= 0 ||
                    separatorIndex >=
                        part.Length - 1)
                {
                    continue;
                }

                string key =
                    part[..separatorIndex];

                string value =
                    part[
                        (separatorIndex + 1)..];

                if (string.Equals(
                        key,
                        "ts",
                        StringComparison.Ordinal))
                {
                    if (!long.TryParse(
                            value,
                            NumberStyles.None,
                            CultureInfo.InvariantCulture,
                            out timestamp))
                    {
                        return false;
                    }
                }
                else if (string.Equals(
                    key,
                    "h1",
                    StringComparison.Ordinal))
                {
                    signatures.Add(
                        value);
                }
            }

            return timestamp > 0 &&
                signatures.Count > 0;
        }
    }
}