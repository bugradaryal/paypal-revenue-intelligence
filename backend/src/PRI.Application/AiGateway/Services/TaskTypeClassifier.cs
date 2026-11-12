namespace PRI.Application.AiGateway.Services;

public static class TaskTypeClassifier
{
    private static readonly string[] ComplexTaskTerms =
    [
        "analy", "analiz", "compare", "karşılaştır", "karsilastir", "audit", "denetle",
        "debug", "hata ayıkla", "hata ayikla", "investigat", "root cause", "kök neden",
        "kok neden", "risk", "strategy", "strateji", "plan", "why", "how", "neden", "nasıl", "nasil"
    ];

    private static readonly string[] SummaryTerms =
    [
        "summar", "summary", "özet", "ozet", "tl;dr"
    ];

    private static readonly string[] SimpleTaskTerms =
    [
        "classif", "categor", "sınıflandır", "siniflandir", "label", "etiket",
        "extract", "çıkar", "cikar", "translate", "çevir", "cevir", "rewrite", "düzenle", "duzenle", "detect"
    ];

    public static string? Classify(string? prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            return null;
        }

        if (ContainsAny(prompt, ComplexTaskTerms))
        {
            return "analysis";
        }

        if (ContainsAny(prompt, SummaryTerms))
        {
            return "summarization";
        }

        return ContainsAny(prompt, SimpleTaskTerms) ? "classification" : null;
    }

    private static bool ContainsAny(string prompt, IEnumerable<string> terms) =>
        terms.Any(term => prompt.Contains(term, StringComparison.OrdinalIgnoreCase));
}
