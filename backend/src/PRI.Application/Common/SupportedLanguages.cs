using System.Globalization;

namespace PRI.Application.Common;

/// <summary>Arayüz dilleri (en, de, tr, es). AI metinleri ve şablonlar bu dillerde üretilir.</summary>
public static class SupportedLanguages
{
    public const string Default = "en";

    public static readonly IReadOnlyList<string> All = ["en", "de", "tr", "es"];

    public static string DisplayName(string language) => language switch
    {
        "de" => "German",
        "tr" => "Turkish",
        "es" => "Spanish",
        _ => "English"
    };

    /// <summary>Accept-Language başlığından desteklenen ilk dili (q değerine göre) seçer; yoksa "en".</summary>
    public static string Resolve(string? acceptLanguage)
    {
        if (string.IsNullOrWhiteSpace(acceptLanguage))
        {
            return Default;
        }

        var ranked = acceptLanguage
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select((part, index) => (Part: part, Index: index))
            .Select(x =>
            {
                var pieces = x.Part.Split(';', StringSplitOptions.TrimEntries);
                var quality = 1d;
                var qPart = pieces.Skip(1).FirstOrDefault(p => p.StartsWith("q=", StringComparison.OrdinalIgnoreCase));
                if (qPart is not null
                    && double.TryParse(qPart[2..], NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
                {
                    quality = parsed;
                }

                var primary = pieces[0].Split('-')[0].ToLowerInvariant();

                return (Language: primary, Quality: quality, x.Index);
            })
            .Where(x => x.Quality > 0)
            .OrderByDescending(x => x.Quality)
            .ThenBy(x => x.Index);

        foreach (var candidate in ranked)
        {
            if (All.Contains(candidate.Language))
            {
                return candidate.Language;
            }
        }

        return Default;
    }
}
