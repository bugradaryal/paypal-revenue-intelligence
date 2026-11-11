namespace PRI.Application.Revenue.Interfaces;

/// <summary>PayPal'ın webhook isteğiyle gönderdiği imza header'ları.</summary>
public sealed record PayPalWebhookHeaders(
    string AuthAlgo,
    string CertUrl,
    string TransmissionId,
    string TransmissionSig,
    string TransmissionTime);

/// <summary>İmzayı PayPal'ın verify-webhook-signature API'siyle doğrular.</summary>
public interface IPayPalWebhookVerifier
{
    Task<bool> VerifyAsync(PayPalWebhookHeaders headers, string rawEventJson, CancellationToken cancellationToken);
}

public interface IPayPalWebhookService
{
    /// <summary>İmzayı doğrular, olayı idempotent olarak kaydeder ve bilinen ödemeleri günceller.</summary>
    Task HandleAsync(PayPalWebhookHeaders headers, string rawEventJson, CancellationToken cancellationToken);
}
