using PRI.Domain.Revenue.Enums;

namespace PRI.Application.Revenue.DTOs;

public sealed class SyncRequest
{
    /// <summary>Geriye dönük kaç gün senkronlanacağı (1-90).</summary>
    public int Days { get; init; } = 30;
}

public sealed record SyncRunDto(
    Guid Id,
    SyncRunStatus Status,
    DateTime StartedAt,
    DateTime? FinishedAt,
    DateTime RangeFrom,
    DateTime RangeTo,
    int ImportedPaymentCount,
    int ImportedInvoiceCount,
    int SkippedCount,
    string? Error);

public sealed record PayPalConnectionDto(
    bool Configured,
    bool Connected,
    string Environment,
    string ApiAuthenticationStatus,
    DateTime? LastSuccessfulSyncAt,
    SyncRunDto? LatestSync,
    string? Error);

public sealed record PayPalTestConnectionDto(
    bool Connected,
    string Environment,
    DateTime TestedAt,
    string? Error);

public sealed record SavePayPalCredentialsRequest(
    string ClientId,
    string ClientSecret,
    string? WebhookId = null);

public sealed record PayPalCredentialsSavedDto(bool Configured);
