using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PRI.Application.Revenue.Interfaces;
using PRI.Shared.Exceptions;

namespace PRI.Infrastructure.Revenue.PayPal;

public sealed class PayPalClient(
    HttpClient http,
    PayPalAccessTokenProvider tokenProvider,
    PayPalCredentialStore credentialStore,
    IOptions<PayPalSettings> options,
    ILogger<PayPalClient> logger,
    TimeProvider timeProvider) : IPayPalClient
{
    private const int PageSize = 100;
    private static readonly TimeSpan MaxSearchRange = TimeSpan.FromDays(31);

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    // PayPal tarihleri "2026-10-01T12:00:00+0000" biçiminde (ofsette iki nokta yok) gelebilir.
    private static readonly Regex OffsetWithoutColon = new(@"([+-]\d{2})(\d{2})$", RegexOptions.Compiled);

    public bool IsConfigured => credentialStore.IsConfigured;

    public bool HasWebhookId => !string.IsNullOrWhiteSpace(credentialStore.WebhookId);

    public string Environment => options.Value.Environment;

    public async Task TestConnectionAsync(CancellationToken cancellationToken)
    {
        var toUtc = timeProvider.GetUtcNow().UtcDateTime.AddMinutes(-1);
        await SearchTransactionsAsync(toUtc.AddMinutes(-5), toUtc, 1, cancellationToken);
    }

    public async Task<PayPalTransactionPage> SearchTransactionsAsync(
        DateTime fromUtc, DateTime toUtc, int page, CancellationToken cancellationToken)
    {
        if (toUtc <= fromUtc || toUtc - fromUtc > MaxSearchRange)
        {
            throw new ArgumentException("PayPal Transaction Search accepts a range of at most 31 days.", nameof(toUtc));
        }

        var url = "v1/reporting/transactions"
            + $"?start_date={FormatQueryDate(fromUtc)}&end_date={FormatQueryDate(toUtc)}"
            + $"&fields=transaction_info,payer_info&page_size={PageSize}&page={page}";

        var response = await GetAsync<TransactionSearchResponse>(url, cancellationToken);

        var items = new List<PayPalTransaction>();
        var skipped = 0;

        foreach (var detail in response.TransactionDetails ?? [])
        {
            var mapped = MapTransaction(detail);
            if (mapped is null)
            {
                skipped++;
                continue;
            }

            items.Add(mapped);
        }

        return new PayPalTransactionPage(items, page, Math.Max(response.TotalPages, 1), skipped);
    }

    public async Task<PayPalInvoicePage> ListInvoicesAsync(int page, CancellationToken cancellationToken)
    {
        var url = $"v2/invoicing/invoices?page={page}&page_size={PageSize}&total_required=true";

        var response = await GetAsync<InvoiceListResponse>(url, cancellationToken);

        var items = new List<PayPalInvoice>();
        var skipped = 0;

        foreach (var item in response.Items ?? [])
        {
            var mapped = MapInvoice(item);
            if (mapped is null)
            {
                skipped++;
                continue;
            }

            items.Add(mapped);
        }

        return new PayPalInvoicePage(items, page, Math.Max(response.TotalPages, 1), skipped);
    }

    public async Task SendInvoiceReminderAsync(string invoiceId, CancellationToken cancellationToken)
    {
        var token = await tokenProvider.GetAccessTokenAsync(cancellationToken);
        var relativeUrl = $"v2/invoicing/invoices/{Uri.EscapeDataString(invoiceId)}/remind";

        // Gövde yalnızca PayPal'ın standart hatırlatma metnini kullanmasını ve satıcıya kopya gitmemesini söyler.
        using var request = new HttpRequestMessage(HttpMethod.Post, relativeUrl)
        {
            Content = JsonContent.Create(new { send_to_invoicer = false })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "PayPal reminder request failed to reach PayPal.");
            throw new ExternalServiceException("paypal_unavailable", "PayPal could not be reached.");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ExternalServiceException("paypal_unavailable", "The PayPal request timed out.");
        }

        using (response)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                tokenProvider.Invalidate();
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogWarning(
                "PayPal returned HTTP {Status} for the invoice reminder: {Body}",
                (int)response.StatusCode, body.Length > 500 ? body[..500] : body);

            AppException failure = response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.NotFound or HttpStatusCode.UnprocessableEntity
                ? new BusinessRuleException(
                    "paypal_rejected_reminder",
                    $"PayPal rejected the reminder (HTTP {(int)response.StatusCode}); the invoice may no longer be payable.")
                : new ExternalServiceException(
                    "paypal_unavailable",
                    $"PayPal returned HTTP {(int)response.StatusCode}. Is 'Invoicing' enabled for the Sandbox app?");

            throw failure;
        }
    }

    // ---------- HTTP ----------

    private async Task<T> GetAsync<T>(string relativeUrl, CancellationToken cancellationToken)
    {
        for (var attempt = 0; ; attempt++)
        {
            var token = await tokenProvider.GetAccessTokenAsync(cancellationToken);

            using var request = new HttpRequestMessage(HttpMethod.Get, relativeUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            HttpResponseMessage response;
            try
            {
                response = await http.SendAsync(request, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                logger.LogWarning(ex, "PayPal request to {Url} failed to reach PayPal.", relativeUrl);
                throw new ExternalServiceException("paypal_unavailable", "PayPal could not be reached.");
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new ExternalServiceException("paypal_unavailable", "The PayPal request timed out.");
            }

            using (response)
            {
                if (attempt == 0
                    && response.StatusCode is (HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden))
                {
                    tokenProvider.Invalidate(token);
                    continue;
                }

                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        tokenProvider.Invalidate(token);
                    }

                    var body = await response.Content.ReadAsStringAsync(cancellationToken);
                    logger.LogWarning(
                        "PayPal returned HTTP {Status} for {Url}: {Body}",
                        (int)response.StatusCode, relativeUrl, body.Length > 500 ? body[..500] : body);

                    var invoiceAccessDenied = response.StatusCode == HttpStatusCode.Forbidden
                                              && relativeUrl.StartsWith("v2/invoicing/invoices", StringComparison.Ordinal);
                    var transactionSearchAccessDenied = response.StatusCode == HttpStatusCode.Forbidden
                                                        && relativeUrl.StartsWith("v1/reporting/transactions", StringComparison.Ordinal);
                    var deniedProduct = invoiceAccessDenied
                        ? "Invoicing"
                        : transactionSearchAccessDenied
                            ? "Transaction Search"
                            : null;
                    var hint = response.StatusCode == HttpStatusCode.Forbidden
                        ? deniedProduct is not null
                            ? $" The Sandbox app does not have {deniedProduct} access."
                            : " Check whether the required PayPal Sandbox API is enabled."
                        : string.Empty;

                    throw new ExternalServiceException(
                        invoiceAccessDenied
                            ? "paypal_invoicing_forbidden"
                            : transactionSearchAccessDenied
                                ? "paypal_transaction_search_forbidden"
                                : "paypal_unavailable",
                        $"PayPal returned HTTP {(int)response.StatusCode}.{hint}");
                }

                var result = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);

                return result ?? throw new ExternalServiceException("paypal_unavailable", "PayPal returned an empty response.");
            }
        }
    }

    // ---------- Mapping ----------

    private static PayPalTransaction? MapTransaction(TransactionDetail detail)
    {
        var info = detail.TransactionInfo;
        if (info is null
            || string.IsNullOrWhiteSpace(info.TransactionId)
            || string.IsNullOrWhiteSpace(info.TransactionStatus)
            || info.TransactionAmount is null
            || string.IsNullOrWhiteSpace(info.TransactionAmount.CurrencyCode)
            || !TryParseDecimal(info.TransactionAmount.Value, out var amount)
            || ParseTimestamp(info.TransactionInitiationDate) is not { } initiatedAt)
        {
            return null;
        }

        var updatedAt = ParseTimestamp(info.TransactionUpdatedDate) ?? initiatedAt;

        return new PayPalTransaction(
            info.TransactionId,
            info.TransactionStatus,
            info.TransactionEventCode,
            amount,
            info.TransactionAmount.CurrencyCode,
            initiatedAt,
            updatedAt,
            Blank(detail.PayerInfo?.AccountId),
            Blank(detail.PayerInfo?.EmailAddress),
            BuildName(detail.PayerInfo?.PayerName),
            Blank(info.InvoiceId),
            Blank(info.PaypalReferenceId),
            Blank(info.PaypalReferenceIdType));
    }

    private static PayPalInvoice? MapInvoice(InvoiceItem item)
    {
        var amount = item.Amount;
        if (string.IsNullOrWhiteSpace(item.Id)
            || string.IsNullOrWhiteSpace(item.Status)
            || amount is null
            || string.IsNullOrWhiteSpace(amount.CurrencyCode)
            || !TryParseDecimal(amount.Value, out var value))
        {
            return null;
        }

        var recipient = item.PrimaryRecipients?.FirstOrDefault()?.BillingInfo;

        return new PayPalInvoice(
            item.Id,
            Blank(item.Detail?.InvoiceNumber),
            item.Status,
            value,
            amount.CurrencyCode,
            ParseDate(item.Detail?.PaymentTerm?.DueDate),
            ParseTimestamp(item.Detail?.Metadata?.CreateTime),
            ParseTimestamp(item.Detail?.Metadata?.LastUpdateTime),
            Blank(recipient?.EmailAddress),
            BuildName(recipient?.Name));
    }

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? BuildName(PayPalPersonName? name)
    {
        if (name is null)
        {
            return null;
        }

        var full = Blank(name.FullName) ?? Blank(name.AlternateFullName);
        if (full is not null)
        {
            return full;
        }

        var combined = $"{name.GivenName} {name.Surname}".Trim();
        return combined.Length == 0 ? null : combined;
    }

    private static bool TryParseDecimal(string? value, out decimal result) =>
        decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);

    private static DateTime? ParseTimestamp(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = OffsetWithoutColon.Replace(value.Trim(), "$1:$2");

        return DateTimeOffset.TryParse(normalized, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)
            ? parsed.UtcDateTime
            : null;
    }

    private static DateOnly? ParseDate(string? value) =>
        DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date : null;

    private static string FormatQueryDate(DateTime utc) =>
        Uri.EscapeDataString(utc.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture));
}
