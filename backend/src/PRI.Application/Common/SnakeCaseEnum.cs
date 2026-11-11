using System.Text.Json;

namespace PRI.Application.Common;

/// <summary>API'de enum'lar snake_case görünür ("payment_pending"); gelen filtre değerlerini enum'a çevirir.</summary>
public static class SnakeCaseEnum
{
    public static bool TryParse<TEnum>(string? value, out TEnum result) where TEnum : struct, Enum
    {
        var wanted = value?.Trim();

        foreach (var candidate in Enum.GetValues<TEnum>())
        {
            var name = JsonNamingPolicy.SnakeCaseLower.ConvertName(candidate.ToString());
            if (string.Equals(name, wanted, StringComparison.OrdinalIgnoreCase))
            {
                result = candidate;
                return true;
            }
        }

        result = default;
        return false;
    }
}
