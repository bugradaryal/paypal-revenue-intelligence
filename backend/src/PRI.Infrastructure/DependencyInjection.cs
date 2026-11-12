using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PRI.Application.AiGateway;
using PRI.Application.AiGateway.Interfaces;
using PRI.Application.Common.Interfaces;
using PRI.Application.Revenue.Interfaces;
using PRI.Domain.AiGateway.Interfaces;
using PRI.Domain.Common.Interfaces;
using PRI.Domain.Revenue.Interfaces;
using PRI.Infrastructure.Persistence;
using PRI.Infrastructure.Persistence.Queries;
using PRI.Infrastructure.AiGateway.Providers.Gemini;
using PRI.Infrastructure.AiGateway.Providers.OpenAi;
using PRI.Infrastructure.AiGateway.Seed;
using PRI.Infrastructure.Auth;
using PRI.Infrastructure.Persistence.Context;
using PRI.Infrastructure.Persistence.Repositories;
using PRI.Infrastructure.Persistence.Seed;
using PRI.Infrastructure.Revenue.PayPal;
using PRI.Shared.Constants;

namespace PRI.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = DatabaseConnectionStringResolver.Resolve(configuration);

        // Proje kuralı: PayPal işlemleri yalnızca Sandbox üzerinden yapılır.
        var paypalEnvironment = configuration[$"{PayPalSettings.SectionName}:Environment"] ?? PayPalEnvironment.Sandbox;
        if (!string.Equals(paypalEnvironment, PayPalEnvironment.Sandbox, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("PayPal:Environment must be 'sandbox'. Only the PayPal Sandbox is supported in this version.");
        }

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<SeedSettings>(configuration.GetSection(SeedSettings.SectionName));
        services.Configure<PayPalSettings>(configuration.GetSection(PayPalSettings.SectionName));
        services.Configure<OpenAiSettings>(configuration.GetSection(OpenAiSettings.SectionName));
        services.Configure<GeminiSettings>(configuration.GetSection(GeminiSettings.SectionName));
        var dataProtection = services.AddDataProtection();
        var keyRingPath = configuration["DataProtection:KeyRingPath"];
        if (!string.IsNullOrWhiteSpace(keyRingPath))
        {
            var keyRingDirectory = new DirectoryInfo(Path.GetFullPath(keyRingPath));
            Directory.CreateDirectory(keyRingDirectory.FullName);
            dataProtection.PersistKeysToFileSystem(keyRingDirectory);
        }
        services.AddSingleton<PayPalCredentialStore>();
        services.AddScoped<IPayPalCredentialService, PayPalCredentialService>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<DatabaseInitializer>();
        services.AddScoped<DemoDataSeeder>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<ISyncRunRepository, SyncRunRepository>();
        services.AddScoped<IWebhookEventRepository, WebhookEventRepository>();
        services.AddScoped<IRevenueQueries, RevenueQueries>();

        services.AddScoped<IProviderModelRepository, ProviderModelRepository>();
        services.AddScoped<IUsageRecordRepository, UsageRecordRepository>();
        services.AddScoped<IGatewayClientRepository, GatewayClientRepository>();
        services.AddScoped<IRoutingRuleRepository, RoutingRuleRepository>();
        services.AddScoped<IGatewayQueries, GatewayQueries>();
        services.AddScoped<IGatewaySpendReader, GatewayQueries>();
        services.AddScoped<IRevenueIssueRepository, RevenueIssueRepository>();
        services.AddScoped<IRecoveryActionRepository, RecoveryActionRepository>();
        services.AddScoped<IAiInsightRepository, AiInsightRepository>();
        services.AddScoped<IRevenueFactsReader, RevenueFactsReader>();
        services.AddScoped<ISemanticCacheRepository, SemanticCacheRepository>();
        services.AddScoped<ICacheLookupRepository, CacheLookupRepository>();
        services.AddScoped<IAiBudgetRepository, AiBudgetRepository>();
        services.AddSingleton(configuration.GetSection(SemanticCacheSettings.SectionName).Get<SemanticCacheSettings>()
                              ?? new SemanticCacheSettings());
        services.AddScoped<GatewaySeeder>();

        services.AddSingleton<IPasswordHasher, IdentityPasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();

        services.AddHttpClient(PayPalAccessTokenProvider.HttpClientName, client =>
        {
            client.BaseAddress = new Uri(PayPalSettings.SandboxBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddSingleton<PayPalAccessTokenProvider>();
        services.AddHttpClient<IPayPalClient, PayPalClient>(client =>
        {
            client.BaseAddress = new Uri(PayPalSettings.SandboxBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddHttpClient<IAiProvider, OpenAiProvider>((serviceProvider, client) =>
        {
            client.BaseAddress = new Uri(OpenAiSettings.DefaultBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(55);   // API sözleşmesi: AI uçları için istemci timeout'u 60 sn
        });

        services.AddHttpClient<IAiProvider, GeminiProvider>(client =>
        {
            client.BaseAddress = new Uri(GeminiSettings.DefaultBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(55);
        });

        services.AddHttpClient<IEmbeddingProvider, OpenAiEmbeddingProvider>((serviceProvider, client) =>
        {
            var settings = serviceProvider.GetRequiredService<IOptions<OpenAiSettings>>().Value;
            client.BaseAddress = new Uri(OpenAiSettings.DefaultBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(20);   // cache araması isteği yavaşlatmasın; zaman aşımında cache'siz devam edilir
        });

        services.AddHttpClient<IPayPalWebhookVerifier, PayPalWebhookVerifier>(client =>
        {
            client.BaseAddress = new Uri(PayPalSettings.SandboxBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddHealthChecks().AddDbContextCheck<AppDbContext>("postgres");

        return services;
    }
}
