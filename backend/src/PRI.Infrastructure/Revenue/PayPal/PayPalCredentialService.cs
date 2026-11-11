using PRI.Application.Revenue.DTOs;
using PRI.Application.Revenue.Interfaces;

namespace PRI.Infrastructure.Revenue.PayPal;

public sealed class PayPalCredentialService(
    PayPalCredentialStore credentialStore,
    PayPalAccessTokenProvider tokenProvider) : IPayPalCredentialService
{
    public async Task<PayPalCredentialsSavedDto> SaveAsync(
        SavePayPalCredentialsRequest request,
        CancellationToken cancellationToken)
    {
        await tokenProvider.ValidateCredentialsAsync(
            request.ClientId.Trim(),
            request.ClientSecret,
            cancellationToken);

        await credentialStore.SaveAsync(
            request.ClientId.Trim(),
            request.ClientSecret,
            request.WebhookId?.Trim() ?? string.Empty,
            cancellationToken);
        tokenProvider.Invalidate();
        return new PayPalCredentialsSavedDto(credentialStore.IsConfigured);
    }
}
