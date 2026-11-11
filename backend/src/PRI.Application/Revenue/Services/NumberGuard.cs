using System.Text.RegularExpressions;

namespace PRI.Application.Revenue.Services;

/// <summary>
/// Yapay zekâ metinlerinde uydurma rakamı engeller: çıktıdaki her sayı, modele verilen olgu metninde de geçmelidir.
/// Finansal hesaplar C# ile yapılır; model yalnızca verilen rakamları anlatabilir.
/// </summary>
public static partial class NumberGuard
{
    public static bool UsesOnlyNumbersFrom(string output, string source)
    {
        var allowed = Extract(source).ToHashSet();

        return Extract(output).All(allowed.Contains);
    }

    private static IEnumerable<string> Extract(string text) =>
        NumberPattern().Matches(text).Select(m => Normalize(m.Value));

    // "125,50" ve "125.50" aynı sayıdır; "125.50" ve "125.5" de öyle.
    private static string Normalize(string token)
    {
        var value = token.Replace(',', '.');

        if (value.Contains('.'))
        {
            value = value.TrimEnd('0').TrimEnd('.');
        }

        return value.Length == 0 ? "0" : value;
    }

    [GeneratedRegex(@"\d+(?:[.,]\d+)*")]
    private static partial Regex NumberPattern();
}
