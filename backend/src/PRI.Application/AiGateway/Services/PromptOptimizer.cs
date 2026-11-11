using System.Text;
using PRI.Application.AiGateway.Interfaces;

namespace PRI.Application.AiGateway.Services;

/// <summary>
/// Anlamı değiştirmeyen, deterministik prompt temizliği (LLM çağrısı yok, maliyet yok):
/// satır sonu normalizasyonu, satır sonu boşluklarının ve görünmez karakterlerin (U+200B, U+2060, U+FEFF) silinmesi,
/// 2'den fazla boş satırın teke indirilmesi, mesaj başı/sonu boşluklarının kırpılması ve tekrar eden system mesajlarının
/// tekilleştirilmesi. Kod blokları (``` veya ~~~) içindeki metne dokunulmaz. Kelime veya içerik silinmez/özetlenmez.
/// </summary>
public sealed class PromptOptimizer : IPromptOptimizer
{
    private static readonly char[] InvisibleCharacters = ['\u200B', '\u2060', '\uFEFF'];

    public OptimizedPrompt Optimize(IReadOnlyList<ProviderMessage> messages)
    {
        var before = messages.Sum(m => m.Content.Length);
        var result = new List<ProviderMessage>(messages.Count);
        var seenSystem = new HashSet<string>(StringComparer.Ordinal);

        foreach (var message in messages)
        {
            var cleaned = Clean(message.Content);

            if (cleaned.Length == 0)
            {
                // Temizlik mesajı tamamen silecekse orijinali koru (içeriği asla kaybetme).
                result.Add(message);
                continue;
            }

            if (message.Role == "system" && !seenSystem.Add(cleaned))
            {
                continue;
            }

            result.Add(message with { Content = cleaned });
        }

        return new OptimizedPrompt(result, before, result.Sum(m => m.Content.Length), true);
    }

    private static string Clean(string content)
    {
        var lines = content.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var builder = new StringBuilder(content.Length);
        var inFence = false;
        var blankRun = 0;

        foreach (var rawLine in lines)
        {
            var trimmedStart = rawLine.TrimStart();
            var isFenceLine = trimmedStart.StartsWith("```", StringComparison.Ordinal)
                              || trimmedStart.StartsWith("~~~", StringComparison.Ordinal);

            string line;
            if (inFence || isFenceLine)
            {
                line = rawLine;
                blankRun = 0;
            }
            else
            {
                line = rawLine.Split(InvisibleCharacters).Length > 1
                    ? string.Concat(rawLine.Split(InvisibleCharacters))
                    : rawLine;
                line = line.TrimEnd();

                if (line.Length == 0)
                {
                    blankRun++;
                    if (blankRun > 1)
                    {
                        continue;
                    }
                }
                else
                {
                    blankRun = 0;
                }
            }

            if (isFenceLine)
            {
                inFence = !inFence;
            }

            builder.Append(line).Append('\n');
        }

        return builder.ToString().Trim();
    }
}
