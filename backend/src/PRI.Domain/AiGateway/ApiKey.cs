using System.Security.Cryptography;
using System.Text;

namespace PRI.Domain.AiGateway;

/// <summary>
/// Üçüncü taraf istemciler için API anahtarı üretimi ve özetleme. Düz anahtar yalnızca oluşturulduğu anda bir kez gösterilir,
/// veritabanında yalnızca SHA-256 özeti saklanır. Anahtarlar yüksek entropili rastgele değerlerdir, bu yüzden yavaş bir
/// parola özeti (bcrypt vb.) gerekmez.
/// </summary>
public static class ApiKey
{
    public const string Prefix = "pri_";
    private const int RandomBytes = 32;
    private const int DisplayPrefixLength = 10;

    public static (string PlainKey, string Hash, string DisplayPrefix) Generate()
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(RandomBytes))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');

        var plainKey = Prefix + token;

        return (plainKey, Hash(plainKey), plainKey[..DisplayPrefixLength]);
    }

    public static string Hash(string plainKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plainKey);

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(plainKey.Trim()))).ToLowerInvariant();
    }
}
