using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using PRI.Application.Revenue.Interfaces;
using PRI.Domain.Common.Interfaces;
using PRI.Domain.Revenue.Entities;
using PRI.Domain.Revenue.Enums;
using PRI.Domain.Revenue.Interfaces;
using PRI.Shared.Exceptions;

namespace PRI.Application.Revenue.Services;

public sealed class PayPalWebhookService(
    IPayPalWebhookVerifier verifier,
    IWebhookEventRepository webhookEvents,
    IPaymentRepository payments,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<PayPalWebhookService> logger) : IPayPalWebhookService
{
    public async Task HandleAsync(PayPalWebhookHeaders headers, string rawEventJson, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(headers.AuthAlgo)
            || string.IsNullOrWhiteSpace(headers.CertUrl)
            || string.IsNullOrWhiteSpace(headers.TransmissionId)
            || string.IsNullOrWhiteSpace(headers.TransmissionSig)
            || string.IsNullOrWhiteSpace(headers.TransmissionTime))
        {
            throw new UnauthorizedException("Missing PayPal webhook signature headers.");
        }

        if (!await verifier.VerifyAsync(headers, rawEventJson, cancellationToken))
        {
            logger.LogWarning("Rejected a PayPal webhook with an invalid signature (transmission {Id}).", headers.TransmissionId);
            throw new UnauthorizedException("Invalid webhook signature.");
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(rawEventJson);
        }
        catch (JsonException)
        {
            throw new RequestValidationException(new Dictionary<string, string[]> { ["body"] = ["The webhook body is not valid JSON."] });
        }

        using (document)
        {
            var root = document.RootElement;
            var eventId = GetString(root, "id");
            var eventType = GetString(root, "event_type");

            if (eventId is null || eventType is null)
            {
                throw new RequestValidationException(new Dictionary<string, string[]> { ["body"] = ["The webhook event has no id or event_type."] });
            }

            // PayPal aynı olayı tekrar gönderebilir; ikinci teslim sessizce kabul edilir.
            if (await webhookEvents.ExistsAsync(eventId, cancellationToken))
            {
                return;
            }

            var now = timeProvider.GetUtcNow().UtcDateTime;
            var webhookEvent = WebhookEvent.Receive(eventId, eventType, rawEventJson, now);

            var outcome = await ProcessAsync(root, eventType, now, cancellationToken);
            webhookEvent.MarkProcessed(outcome, now);

            webhookEvents.Add(webhookEvent);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation("PayPal webhook {EventId} ({EventType}) processed: {Outcome}", eventId, eventType, outcome);
        }
    }

    private async Task<string> ProcessAsync(JsonElement root, string eventType, DateTime now, CancellationToken cancellationToken)
    {
        PaymentStatus status;
        switch (eventType)
        {
            case "PAYMENT.CAPTURE.COMPLETED":
                status = PaymentStatus.Completed;
                break;
            case "PAYMENT.CAPTURE.PENDING":
                status = PaymentStatus.Pending;
                break;
            case "PAYMENT.CAPTURE.DENIED":
            case "PAYMENT.CAPTURE.DECLINED":
                status = PaymentStatus.Failed;
                break;
            case "PAYMENT.CAPTURE.REFUNDED":
            case "PAYMENT.CAPTURE.REVERSED":
                status = PaymentStatus.Refunded;
                break;
            default:
                // Fatura olayları vb. kaydedilir; veri bir sonraki senkronda güncellenir.
                return "ignored_event_type";
        }

        if (!root.TryGetProperty("resource", out var resource) || resource.ValueKind != JsonValueKind.Object)
        {
            return "no_resource";
        }

        var captureId = eventType == "PAYMENT.CAPTURE.REFUNDED"
            ? CaptureIdFromUpLink(resource)   // iade olayında resource iadedir; orijinal capture "up" linkindedir
            : GetString(resource, "id");

        if (captureId is null)
        {
            return "no_capture_id";
        }

        var found = await payments.GetByExternalIdsAsync(DataSource.Paypal, [captureId], cancellationToken);
        if (!found.TryGetValue(captureId, out var payment))
        {
            // Webhook yeni bir ödemeyi tek başına oluşturmaz (müşteri bilgisi yok); sonraki senkron içe alır.
            return "payment_not_synced";
        }

        var updatedAt = ParseTimestamp(GetString(resource, "update_time") ?? GetString(resource, "create_time")) ?? now;

        return payment.ApplyProviderUpdate(status, null, updatedAt, now) ? "payment_updated" : "no_change";
    }

    private static string? CaptureIdFromUpLink(JsonElement resource)
    {
        if (!resource.TryGetProperty("links", out var links) || links.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var link in links.EnumerateArray())
        {
            if (GetString(link, "rel") != "up")
            {
                continue;
            }

            var href = GetString(link, "href");
            if (href is null)
            {
                continue;
            }

            var id = href.TrimEnd('/').Split('/').LastOrDefault();
            if (!string.IsNullOrWhiteSpace(id))
            {
                return id;
            }
        }

        return null;
    }

    private static string? GetString(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object
        && element.TryGetProperty(name, out var value)
        && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static DateTime? ParseTimestamp(string? value) =>
        DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)
            ? parsed.UtcDateTime
            : null;
}
