namespace PRI.Domain.Revenue.Enums;

/// <summary>Tespit edilen gelir kaybı türü. JSON'da failed_payment / pending_payment / overdue_invoice.</summary>
public enum IssueType
{
    FailedPayment = 1,
    PendingPayment = 2,
    OverdueInvoice = 3
}

public enum IssueStatus
{
    Open = 1,
    Recovered = 2,

    /// <summary>Artık geçerli değil (iade, iptal, vade uzatıldı, bekleyen ödeme başarısız oldu vb.); kayıp/kurtarma hesabına girmez.</summary>
    Void = 3
}

/// <summary>Kaybın neden "kurtarıldı" sayıldığının kanıtı.</summary>
public enum RecoveryEvidence
{
    /// <summary>Ödeme tamamlandı veya fatura ödendi (kaynaktaki durum değişti).</summary>
    StatusChanged = 1,

    /// <summary>Aynı faturaya bağlı, başarısız ödemeden sonra başlamış tamamlanmış başka bir ödeme var.</summary>
    LinkedInvoicePayment = 2,

    /// <summary>Aynı müşteri, aynı tutar ve para biriminde, eşleme penceresi içinde tamamlanmış yeni bir ödeme var (kesin kanıt değil, kural tabanlı eşleme).</summary>
    MatchingRetryPayment = 3
}

public enum RecoveryActionType
{
    SendReminder = 1,
    RetryPayment = 2,
    OfferAlternativeMethod = 3
}

public enum RecoveryActionStatus
{
    Proposed = 1,
    Approved = 2,
    Rejected = 3,
    Executed = 4,
    Failed = 5
}

/// <summary>Bir metnin nasıl üretildiği. JSON'da "rule" / "ai".</summary>
public enum ContentSource
{
    Rule = 1,
    Ai = 2
}
