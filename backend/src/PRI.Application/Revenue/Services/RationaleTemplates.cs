using System.Globalization;
using PRI.Domain.Revenue.Enums;

namespace PRI.Application.Revenue.Services;

/// <summary>Yapay zekâ kullanılamadığında (kapalı, bütçe doldu, doğrulama başarısız) kullanılan, yalnızca olgulardan kurulan şablon metinler.</summary>
public static class RationaleTemplates
{
    public static string Build(RecoveryActionType type, decimal amount, string currency, PaymentFailureCode? failureCode, int days, string language)
    {
        var lang = Texts.ContainsKey(language) ? language : "en";
        var t = Texts[lang];
        var money = $"{amount.ToString("0.00", CultureInfo.InvariantCulture)} {currency}";

        return type switch
        {
            RecoveryActionType.SendReminder => string.Format(CultureInfo.InvariantCulture, t.Reminder, money, days),
            RecoveryActionType.RetryPayment => string.Format(CultureInfo.InvariantCulture, t.Retry, money, Reason(t, failureCode), days),
            _ => string.Format(CultureInfo.InvariantCulture, t.Alternative, money, Reason(t, failureCode), days)
        };
    }

    public static string Insight(decimal revenue, decimal failed, double failureRate, double recoveryRate, int days, string language)
    {
        var t = Texts[Texts.ContainsKey(language) ? language : "en"];

        return string.Format(
            CultureInfo.InvariantCulture, t.Insight,
            days, revenue.ToString("0.00", CultureInfo.InvariantCulture), failed.ToString("0.00", CultureInfo.InvariantCulture),
            (failureRate * 100).ToString("0.0", CultureInfo.InvariantCulture),
            (recoveryRate * 100).ToString("0.0", CultureInfo.InvariantCulture));
    }

    private static string Reason(LanguageTexts t, PaymentFailureCode? code) =>
        code switch
        {
            PaymentFailureCode.CardDeclined => t.CardDeclined,
            PaymentFailureCode.InsufficientFunds => t.InsufficientFunds,
            PaymentFailureCode.ExpiredCard => t.ExpiredCard,
            PaymentFailureCode.AccountRestricted => t.AccountRestricted,
            _ => t.OtherReason
        };

    private sealed record LanguageTexts(
        string Reminder, string Retry, string Alternative, string Insight,
        string CardDeclined, string InsufficientFunds, string ExpiredCard, string AccountRestricted, string OtherReason);

    private static readonly Dictionary<string, LanguageTexts> Texts = new()
    {
        ["en"] = new(
            "This {0} item has been outstanding for {1} day(s). A payment reminder is the lowest-effort way to collect it.",
            "This {0} payment failed ({1}) {2} day(s) ago. The cause is often temporary, so retrying the charge may succeed.",
            "This {0} payment failed ({1}) {2} day(s) ago. Retrying the same method is unlikely to help, so offer the customer another payment method.",
            "Last {0} days: {1} USD collected, {2} USD failed (failure rate {3}%), recovery rate {4}%.",
            "card declined", "insufficient funds", "card expired", "account restricted", "unspecified reason"),
        ["tr"] = new(
            "Bu {0} tutarındaki kalem {1} gündür tahsil edilemedi. Ödeme hatırlatması, tahsil etmenin en az emek gerektiren yoludur.",
            "Bu {0} tutarındaki ödeme {2} gün önce başarısız oldu ({1}). Neden çoğu zaman geçicidir; ödemeyi yeniden denemek işe yarayabilir.",
            "Bu {0} tutarındaki ödeme {2} gün önce başarısız oldu ({1}). Aynı yöntemi tekrar denemek büyük olasılıkla işe yaramaz; müşteriye başka bir ödeme yöntemi sunun.",
            "Son {0} gün: {1} USD tahsil edildi, {2} USD başarısız oldu (başarısızlık oranı %{3}), kurtarma oranı %{4}.",
            "kart reddedildi", "yetersiz bakiye", "kartın süresi dolmuş", "hesap kısıtlı", "belirtilmemiş neden"),
        ["de"] = new(
            "Dieser Posten über {0} ist seit {1} Tag(en) offen. Eine Zahlungserinnerung ist der einfachste Weg, ihn einzuziehen.",
            "Diese Zahlung über {0} ist vor {2} Tag(en) fehlgeschlagen ({1}). Die Ursache ist oft vorübergehend, ein erneuter Versuch kann klappen.",
            "Diese Zahlung über {0} ist vor {2} Tag(en) fehlgeschlagen ({1}). Dieselbe Methode erneut zu versuchen hilft wohl nicht; bieten Sie dem Kunden eine andere Zahlungsmethode an.",
            "Letzte {0} Tage: {1} USD eingenommen, {2} USD fehlgeschlagen (Fehlerquote {3} %), Rückgewinnungsquote {4} %.",
            "Karte abgelehnt", "unzureichende Deckung", "Karte abgelaufen", "Konto eingeschränkt", "unbekannter Grund"),
        ["es"] = new(
            "Este importe de {0} lleva {1} día(s) pendiente. Un recordatorio de pago es la forma más sencilla de cobrarlo.",
            "Este pago de {0} falló ({1}) hace {2} día(s). La causa suele ser temporal, así que reintentar el cobro puede funcionar.",
            "Este pago de {0} falló ({1}) hace {2} día(s). Es poco probable que reintentar el mismo método ayude; ofrezca al cliente otro método de pago.",
            "Últimos {0} días: {1} USD cobrados, {2} USD fallidos (tasa de fallos {3} %), tasa de recuperación {4} %.",
            "tarjeta rechazada", "fondos insuficientes", "tarjeta caducada", "cuenta restringida", "motivo no especificado")
    };
}
