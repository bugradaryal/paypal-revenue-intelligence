using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PRI.Application.AiGateway.Interfaces;
using PRI.Application.Revenue.Interfaces;
using PRI.Infrastructure.Persistence.Context;

namespace PRI.Api.IntegrationTests;

/// <summary>
/// Gerçek API'yi (tüm katmanlar, gerçek PostgreSQL, gerçek migration'lar, açılış seed'i ve demo veri) ayağa kaldırır; yalnızca
/// dış dünyaya çıkan üç kapıyı (OpenAI sohbet, OpenAI embedding, PayPal) sahtelerle değiştirir.
/// Ortam hazırlığı ilk kullanımda yapılır, böylece testler atlandığında veritabanına hiç dokunulmaz.
/// </summary>
public sealed class PriApiFactory : WebApplicationFactory<Program>
{
    private readonly object _gate = new();
    private bool _prepared;

    public ScriptedAiProvider Ai { get; } = new();

    public ControllableEmbeddingProvider Embeddings { get; } = new();

    public RecordingPayPalClient PayPal { get; } = new();

    public IServiceProvider AppServices
    {
        get
        {
            Prepare();

            return Services;
        }
    }

    /// <summary>Admin olarak giriş yapmış istemci (açılışta seed edilen geliştirme kullanıcısı).</summary>
    public async Task<HttpClient> CreateAdminClientAsync()
    {
        Prepare();
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = "admin@pri.local", password = "Admin123!" });
        response.EnsureSuccessStatusCode();

        var token = (await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>()).GetProperty("accessToken").GetString();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        return client;
    }

    public HttpClient CreateAnonymousClient()
    {
        Prepare();

        return CreateClient();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAiProvider>();
            services.RemoveAll<IEmbeddingProvider>();
            services.RemoveAll<IPayPalClient>();

            services.AddSingleton<IAiProvider>(Ai);
            services.AddSingleton<IEmbeddingProvider>(Embeddings);
            services.AddSingleton<IPayPalClient>(PayPal);
        });
    }

    private void Prepare()
    {
        lock (_gate)
        {
            if (_prepared)
            {
                return;
            }

            // WebApplication.CreateBuilder ortam değişkenlerini okur; ana host kurulmadan önce ayarlanmalıdır.
            Environment.SetEnvironmentVariable("ConnectionStrings__Default", IntegrationEnvironment.ConnectionString);
            Environment.SetEnvironmentVariable("Database__InitializeOnStartup", "true");
            Environment.SetEnvironmentVariable("Seed__DemoData", "true");

            // Temiz başlangıç: test veritabanını sil; açılışta migration'lar uygulanıp seed çalışır.
            using (var db = new AppDbContext(
                       new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(IntegrationEnvironment.ConnectionString).Options))
            {
                db.Database.EnsureDeleted();
            }

            _prepared = true;
        }
    }
}
