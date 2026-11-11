using Microsoft.Extensions.Logging;
using PRI.Application.Revenue.Interfaces;
using PRI.Domain.Common.Interfaces;
using PRI.Domain.Revenue.Enums;
using PRI.Domain.Revenue.Interfaces;
using PRI.Shared.Exceptions;

namespace PRI.Application.Revenue.Services;

/// <summary>
/// Onaylanmış ve çalıştırılabilir bir öneriyi uygular. Şu an yalnızca PayPal faturası için hatırlatma gönderilebilir;
/// sonuç Executed veya Failed (hata koduyla) olarak kaydedilir. Onaylı olmayan öneriler asla çalıştırılmaz.
/// </summary>
public sealed class RecoveryActionExecutor(
    IRecoveryActionRepository actions,
    IRevenueFactsReader facts,
    IPayPalClient payPal,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<RecoveryActionExecutor> logger) : IRecoveryActionExecutor
{
    public async Task ExecuteAsync(Guid actionId, CancellationToken cancellationToken)
    {
        var action = await actions.GetByIdAsync(actionId, cancellationToken);
        if (action is null || action.Status != RecoveryActionStatus.Approved || !action.Executable)
        {
            return;
        }

        string? error = null;

        try
        {
            if (action.Type != RecoveryActionType.SendReminder || action.InvoiceId is not { } invoiceId)
            {
                error = "unsupported_action";
            }
            else
            {
                var invoice = (await facts.ListInvoicesByIdsAsync([invoiceId], cancellationToken)).FirstOrDefault();

                if (invoice is null || invoice.DataSource != DataSource.Paypal)
                {
                    error = "invoice_not_found";
                }
                else if (invoice.Status is not (InvoiceStatus.Sent or InvoiceStatus.PartiallyPaid))
                {
                    error = "invoice_not_remindable";
                }
                else
                {
                    await payPal.SendInvoiceReminderAsync(invoice.ExternalId, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (AppException ex)
        {
            logger.LogWarning("Executing suggestion {ActionId} failed: {Code}", actionId, ex.Code);
            error = ex.Code;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Executing suggestion {ActionId} failed unexpectedly.", actionId);
            error = "internal_error";
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;

        if (error is null)
        {
            action.MarkExecuted(now);
        }
        else
        {
            action.MarkFailed(error, now);
        }

        await unitOfWork.SaveChangesAsync(CancellationToken.None);
    }
}
