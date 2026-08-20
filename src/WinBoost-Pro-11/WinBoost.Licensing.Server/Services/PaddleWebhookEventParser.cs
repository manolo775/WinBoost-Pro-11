using System;
using System.Text.Json;

namespace WinBoost.Licensing.Server.Services
{
    public sealed class PaddleWebhookEventParser
    {
        public bool TryParseTransactionCompleted(
            string rawBody,
            out string transactionId,
            out string eventId,
            out string failureReason)
        {
            transactionId =
                string.Empty;

            eventId =
                string.Empty;

            failureReason =
                string.Empty;

            if (string.IsNullOrWhiteSpace(
                    rawBody))
            {
                failureReason =
                    "EMPTY_BODY";

                return false;
            }

            try
            {
                using JsonDocument document =
                    JsonDocument.Parse(
                        rawBody);

                JsonElement root =
                    document.RootElement;

                if (!root.TryGetProperty(
                        "event_type",
                        out JsonElement eventTypeElement))
                {
                    failureReason =
                        "EVENT_TYPE_MISSING";

                    return false;
                }

                string? eventType =
                    eventTypeElement.GetString();

                if (!string.Equals(
                        eventType,
                        "transaction.completed",
                        StringComparison.Ordinal))
                {
                    failureReason =
                        "UNSUPPORTED_EVENT_TYPE";

                    return false;
                }

                if (root.TryGetProperty(
                        "event_id",
                        out JsonElement eventIdElement))
                {
                    eventId =
                        eventIdElement.GetString()
                        ?? string.Empty;
                }

                if (!root.TryGetProperty(
                        "data",
                        out JsonElement dataElement))
                {
                    failureReason =
                        "DATA_MISSING";

                    return false;
                }

                if (!dataElement.TryGetProperty(
                        "id",
                        out JsonElement transactionIdElement))
                {
                    failureReason =
                        "TRANSACTION_ID_MISSING";

                    return false;
                }

                transactionId =
                    transactionIdElement.GetString()
                    ?? string.Empty;

                if (string.IsNullOrWhiteSpace(
                        transactionId))
                {
                    failureReason =
                        "TRANSACTION_ID_EMPTY";

                    return false;
                }

                failureReason =
                    "VALID";

                return true;
            }
            catch (JsonException)
            {
                failureReason =
                    "INVALID_JSON";

                return false;
            }
        }
    }
}