using PRI.Application.Revenue.DTOs;

namespace PRI.Application.Revenue.Interfaces;

public interface IPayPalCredentialService
{
    Task<PayPalCredentialsSavedDto> SaveAsync(
        SavePayPalCredentialsRequest request,
        CancellationToken cancellationToken);
}
