using System.Globalization;

namespace PRI.Application.Common.DTOs;

/// <summary>API sözleşmesindeki para biçimi: ondalık string + ISO 4217 para birimi.</summary>
public sealed record MoneyDto(string Amount, string Currency)
{
    /// <summary>Ödeme tutarları: 2 ondalık basamak.</summary>
    public static MoneyDto From(decimal amount, string currency) =>
        new(amount.ToString("0.00", CultureInfo.InvariantCulture), currency);

    /// <summary>AI maliyetleri ve token fiyatları: 6 ondalık basamak (tek bir istek çoğu zaman bir sentin kesridir).</summary>
    public static MoneyDto FromPrecise(decimal amount, string currency) =>
        new(amount.ToString("0.000000", CultureInfo.InvariantCulture), currency);
}
