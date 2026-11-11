using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace PRI.Domain.AiGateway;

/// <summary>
/// Semantic cache anahtarları. Bir yanıt yalnızca (aynı istemci, aynı bağlam) içinde yeniden kullanılır. Bağlam: model isteği,
/// görev tipi, üretim parametreleri ve son kullanıcı mesajından önceki tüm mesajlar (system prompt dahil).
/// Son kullanıcı mesajı ise tam eşleşme (QueryHash) veya embedding benzerliği ile aranır.
/// </summary>
public static partial class SemanticCacheKey
{
    public static string NormalizeQuery(string text) =>
        WhitespaceRun().Replace(text.Trim(), " ").ToLowerInvariant();

    public static string ComputeQueryHash(string normalizedQuery) => Sha256(normalizedQuery);

    /// <summary>
    /// Sorgudaki sayı dizilerinin (sırasıyla) özeti. Anlamsal olarak çok benzeyen ama sayısı farklı sorular
    /// ("sipariş 123 / sipariş 456") aynı yanıtı paylaşmasın diye benzerlik eşleşmesinde ek koşul olarak aranır.
    /// </summary>
    public static string ComputeNumbersFingerprint(string text)
    {
        var numbers = AsciiNumber().Matches(text).Select(m => m.Value);

        return Sha256(string.Join(',', numbers));
    }

    public static string ComputeContextHash(
        string? requestedModel,
        string? taskType,
        int? maxOutputTokens,
        double? temperature,
        string? reasoningEffort,
        IEnumerable<(string Role, string Content)> precedingMessages)
    {
        var builder = new StringBuilder();

        Append(builder, string.IsNullOrWhiteSpace(requestedModel) ? "auto" : requestedModel.Trim().ToLowerInvariant());
        Append(builder, taskType?.Trim().ToLowerInvariant() ?? string.Empty);
        Append(builder, maxOutputTokens?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty);
        Append(builder, temperature?.ToString("R", System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty);
        Append(builder, reasoningEffort ?? string.Empty);

        foreach (var (role, content) in precedingMessages)
        {
            Append(builder, role);
            Append(builder, content);
        }

        return Sha256(builder.ToString());
    }

    // Uzunluk önekli birleştirme: alanlar birbirine kaydırılarak aynı özet üretilemez.
    private static void Append(StringBuilder builder, string value) =>
        builder.Append(value.Length).Append(':').Append(value).Append('|');

    private static string Sha256(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRun();

    [GeneratedRegex("[0-9]+")]
    private static partial Regex AsciiNumber();
}
