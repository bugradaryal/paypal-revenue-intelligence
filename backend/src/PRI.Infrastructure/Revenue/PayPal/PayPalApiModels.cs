namespace PRI.Infrastructure.Revenue.PayPal;

// PayPal JSON modelleri (snake_case, JsonNamingPolicy.SnakeCaseLower ile eşlenir). Yalnızca kullandığımız alanlar tanımlıdır.

internal sealed class TokenResponse
{
    public string? AccessToken { get; set; }

    public int ExpiresIn { get; set; }

    public string? Scope { get; set; }
}

internal sealed class PayPalAmount
{
    public string? CurrencyCode { get; set; }

    public string? Value { get; set; }
}

internal sealed class PayPalPersonName
{
    public string? GivenName { get; set; }

    public string? Surname { get; set; }

    public string? AlternateFullName { get; set; }

    public string? FullName { get; set; }
}

// ----- Transaction Search (GET /v1/reporting/transactions) -----

internal sealed class TransactionSearchResponse
{
    public List<TransactionDetail>? TransactionDetails { get; set; }

    public int Page { get; set; }

    public int TotalPages { get; set; }
}

internal sealed class TransactionDetail
{
    public TransactionInfo? TransactionInfo { get; set; }

    public PayerInfo? PayerInfo { get; set; }
}

internal sealed class TransactionInfo
{
    public string? TransactionId { get; set; }

    public string? TransactionEventCode { get; set; }

    public string? TransactionInitiationDate { get; set; }

    public string? TransactionUpdatedDate { get; set; }

    public PayPalAmount? TransactionAmount { get; set; }

    public string? TransactionStatus { get; set; }

    public string? PaypalReferenceId { get; set; }

    public string? PaypalReferenceIdType { get; set; }

    public string? InvoiceId { get; set; }
}

internal sealed class PayerInfo
{
    public string? AccountId { get; set; }

    public string? EmailAddress { get; set; }

    public PayPalPersonName? PayerName { get; set; }
}

// ----- Invoicing (GET /v2/invoicing/invoices) -----

internal sealed class InvoiceListResponse
{
    public List<InvoiceItem>? Items { get; set; }

    public int TotalPages { get; set; }
}

internal sealed class InvoiceItem
{
    public string? Id { get; set; }

    public string? Status { get; set; }

    public InvoiceDetail? Detail { get; set; }

    public List<InvoiceRecipient>? PrimaryRecipients { get; set; }

    public PayPalAmount? Amount { get; set; }
}

internal sealed class InvoiceDetail
{
    public string? InvoiceNumber { get; set; }

    public string? InvoiceDate { get; set; }

    public string? CurrencyCode { get; set; }

    public InvoicePaymentTerm? PaymentTerm { get; set; }

    public InvoiceMetadata? Metadata { get; set; }
}

internal sealed class InvoicePaymentTerm
{
    public string? DueDate { get; set; }
}

internal sealed class InvoiceMetadata
{
    public string? CreateTime { get; set; }

    public string? LastUpdateTime { get; set; }
}

internal sealed class InvoiceRecipient
{
    public InvoiceBillingInfo? BillingInfo { get; set; }
}

internal sealed class InvoiceBillingInfo
{
    public PayPalPersonName? Name { get; set; }

    public string? EmailAddress { get; set; }
}
