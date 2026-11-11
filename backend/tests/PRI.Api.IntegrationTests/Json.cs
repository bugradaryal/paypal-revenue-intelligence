using System.Net.Http.Json;
using System.Text.Json;

namespace PRI.Api.IntegrationTests;

internal static class Json
{
    public static async Task<JsonElement> ReadAsync(this HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();

    /// <summary>Başarılı bir yanıtı okur; başarısızsa gövdeyi hata iletisine koyar (testte neyin yanlış gittiği görünsün).</summary>
    public static async Task<JsonElement> ReadOkAsync(this HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw new Xunit.Sdk.XunitException(
                $"Expected success but got {(int)response.StatusCode} {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        }

        return await response.ReadAsync();
    }

    public static string Str(this JsonElement element, string property) => element.GetProperty(property).GetString()!;

    public static IEnumerable<JsonElement> Items(this JsonElement page) => page.GetProperty("items").EnumerateArray();

    public static decimal Amount(this JsonElement money) =>
        decimal.Parse(money.GetProperty("amount").GetString()!, System.Globalization.CultureInfo.InvariantCulture);
}
