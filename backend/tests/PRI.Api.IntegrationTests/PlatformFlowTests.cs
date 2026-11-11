using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using PRI.Domain.Revenue.Entities;
using PRI.Domain.Revenue.Enums;
using PRI.Infrastructure.Persistence.Context;
using PRI.Shared.Exceptions;

namespace PRI.Api.IntegrationTests;

/// <summary>
/// Uçtan uca akışlar: gerçek HTTP → controller → servis → PostgreSQL. OpenAI ve PayPal sahtedir.
/// Testler tek bir veritabanını paylaşır ve sırayla çalışır; her test kendi verisini (benzersiz metinler, benzersiz faturalar) kullanır.
/// </summary>
public class PlatformFlowTests(PriApiFactory factory) : IClassFixture<PriApiFactory>
{
    private static string Range(int days = 90)
    {
        var to = DateTime.UtcNow.Date.AddDays(1);
        var from = to.AddDays(-days);

        return $"from={Uri.EscapeDataString(from.ToString("o"))}&to={Uri.EscapeDataString(to.ToString("o"))}";
    }

    private static async Task<HttpResponseMessage> PostAsync(HttpClient client, string url, object? body = null, string? language = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        if (language is not null)
        {
            request.Headers.AcceptLanguage.ParseAdd(language);
        }

        return await client.SendAsync(request);
    }

    private static Task<HttpResponseMessage> PutAsync(HttpClient client, string url, object body) =>
        client.PutAsJsonAsync(url, body);

    private static async Task<JsonElement> CompleteAsync(HttpClient client, string question, bool? cache = null)
    {
        var response = await PostAsync(client, "/api/v1/gateway/completions", new
        {
            messages = new[] { new { role = "user", content = question } },
            cache
        });

        return await response.ReadOkAsync();
    }

    // ---------- altyapı ve kimlik doğrulama ----------

    [IntegrationFact]
    public async Task Health_and_ping_work_without_a_token()
    {
        var client = factory.CreateAnonymousClient();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);

        var ping = await (await client.GetAsync("/api/v1/system/ping")).ReadOkAsync();
        Assert.Equal("sandbox", ping.Str("paypalEnvironment"));
    }

    [IntegrationFact]
    public async Task Protected_endpoints_reject_anonymous_callers_and_wrong_passwords()
    {
        var client = factory.CreateAnonymousClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/revenue/payments")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/gateway/budgets")).StatusCode);

        var bad = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = "admin@pri.local", password = "wrong-password" });
        Assert.Equal(HttpStatusCode.Unauthorized, bad.StatusCode);
    }

    [IntegrationFact]
    public async Task Login_returns_a_working_token_for_the_seeded_admin()
    {
        var client = await factory.CreateAdminClientAsync();

        var me = await (await client.GetAsync("/api/v1/auth/me")).ReadOkAsync();

        Assert.Equal("admin@pri.local", me.Str("email"));
        Assert.Equal("admin", me.Str("role"));
    }

    // ---------- demo veri ----------

    [IntegrationFact]
    public async Task Demo_data_is_seeded_labeled_as_demo_and_visible_through_the_revenue_endpoints()
    {
        var client = await factory.CreateAdminClientAsync();

        var payments = await (await client.GetAsync("/api/v1/revenue/payments?pageSize=100")).ReadOkAsync();
        var invoices = await (await client.GetAsync("/api/v1/revenue/invoices?pageSize=100")).ReadOkAsync();
        var summary = await (await client.GetAsync($"/api/v1/revenue/summary?{Range()}")).ReadOkAsync();

        Assert.True(payments.GetProperty("totalItems").GetInt32() > 100);
        Assert.All(payments.Items(), p => Assert.Equal("demo", p.Str("dataSource")));
        Assert.All(invoices.Items(), i => Assert.Equal("demo", i.Str("dataSource")));
        Assert.True(summary.GetProperty("totalRevenue").Amount() > 0);
        Assert.True(summary.GetProperty("excludedPaymentCount").GetInt32() >= 2);   // USD dışı iki demo ödeme
    }

    // ---------- kayıp tespiti, öneriler, onay ----------

    [IntegrationFact]
    public async Task Generating_suggestions_detects_losses_writes_localized_rationales_and_enforces_one_decision()
    {
        var client = await factory.CreateAdminClientAsync();

        var generated = await (await PostAsync(client, "/api/v1/revenue/suggestions/generate", language: "tr-TR,tr;q=0.9,en;q=0.8")).ReadOkAsync();
        var created = generated.GetProperty("createdCount").GetInt32();
        Assert.InRange(created, 1, 10);

        var list = await (await client.GetAsync("/api/v1/revenue/suggestions?status=proposed&pageSize=100")).ReadOkAsync();
        var suggestions = list.Items().ToList();
        Assert.True(suggestions.Count >= created);
        Assert.All(suggestions, s => Assert.Equal("tr", s.Str("rationaleLanguage")));
        Assert.All(suggestions, s => Assert.False(string.IsNullOrWhiteSpace(s.Str("rationale"))));
        // Sahte AI sayı içermeyen metin döndürür, bu yüzden doğrulamadan geçer.
        Assert.Contains(suggestions, s => s.Str("rationaleSource") == "ai");
        // Demo kayıtlar için hiçbir aksiyon çalıştırılabilir değildir.
        Assert.All(suggestions.Where(s => s.Str("dataSource") == "demo"), s => Assert.False(s.GetProperty("executable").GetBoolean()));

        var target = suggestions.First(s => !s.GetProperty("executable").GetBoolean());
        var id = target.Str("id");

        var approved = await (await PostAsync(client, $"/api/v1/revenue/suggestions/{id}/decision", new { decision = "approve" })).ReadOkAsync();
        Assert.Equal("approved", approved.Str("status"));

        var second = await PostAsync(client, $"/api/v1/revenue/suggestions/{id}/decision", new { decision = "reject" });
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal("suggestion_already_decided", (await second.ReadAsync()).Str("code"));

        // Kurtarma: demo verideki 4 yeniden deneme + 2 faturaya bağlı kayıp kurtarılmış sayılmalı.
        var recovery = await (await client.GetAsync($"/api/v1/revenue/recovery/summary?{Range()}")).ReadOkAsync();
        Assert.True(recovery.GetProperty("lostAmount").Amount() > 0);
        Assert.True(recovery.GetProperty("recoveredAmount").Amount() > 0);
        Assert.InRange(recovery.GetProperty("recoveryRate").GetDouble(), 0.0001, 1.0);

        var recovered = await (await client.GetAsync("/api/v1/revenue/issues?status=recovered&pageSize=100")).ReadOkAsync();
        Assert.True(recovered.GetProperty("totalItems").GetInt32() >= 6);
        Assert.Contains(recovered.Items(), i => i.Str("evidence") == "matching_retry_payment");
        Assert.Contains(recovered.Items(), i => i.Str("evidence") == "linked_invoice_payment");

        var summary = await (await client.GetAsync($"/api/v1/revenue/summary?{Range()}")).ReadOkAsync();
        Assert.True(summary.GetProperty("recoveredRevenue").Amount() > 0);
    }

    [IntegrationFact]
    public async Task Two_simultaneous_decisions_on_the_same_suggestion_yield_exactly_one_success()
    {
        var client = await factory.CreateAdminClientAsync();

        var id = await FindProposedAdviceOnlySuggestionAsync(client);
        var results = await Task.WhenAll(
            Enumerable.Range(0, 2).Select(_ => PostAsync(client, $"/api/v1/revenue/suggestions/{id}/decision", new { decision = "approve" })));

        Assert.Equal(new[] { 200, 409 }, results.Select(r => (int)r.StatusCode).OrderBy(x => x).ToArray());

        var conflict = await results.Single(r => r.StatusCode == HttpStatusCode.Conflict).ReadAsync();
        Assert.Contains(conflict.Str("code"), new[] { "suggestion_already_decided", "concurrent_update" });
    }

    private async Task<string> FindProposedAdviceOnlySuggestionAsync(HttpClient client)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var list = await (await client.GetAsync("/api/v1/revenue/suggestions?status=proposed&pageSize=100")).ReadOkAsync();
            var candidate = list.Items().FirstOrDefault(s => !s.GetProperty("executable").GetBoolean());
            if (candidate.ValueKind == JsonValueKind.Object)
            {
                return candidate.Str("id");
            }

            (await PostAsync(client, "/api/v1/revenue/suggestions/generate")).EnsureSuccessStatusCode();
        }

        throw new Xunit.Sdk.XunitException("No proposed advice-only suggestion could be found or generated.");
    }

    [IntegrationFact]
    public async Task An_approved_paypal_reminder_is_executed_in_the_background_and_a_paypal_error_ends_as_failed()
    {
        var client = await factory.CreateAdminClientAsync();
        var (okInvoiceId, failingInvoiceId) = await AddOverduePayPalInvoicesAsync();

        (await PostAsync(client, "/api/v1/revenue/suggestions/generate")).EnsureSuccessStatusCode();

        var proposed = (await (await client.GetAsync("/api/v1/revenue/suggestions?status=proposed&pageSize=100")).ReadOkAsync()).Items().ToList();
        var okSuggestion = proposed.Single(s => s.GetProperty("invoiceId").GetString() == okInvoiceId.ToString());
        var failingSuggestion = proposed.Single(s => s.GetProperty("invoiceId").GetString() == failingInvoiceId.ToString());
        Assert.True(okSuggestion.GetProperty("executable").GetBoolean());
        Assert.Equal("send_reminder", okSuggestion.Str("type"));
        Assert.Equal("overdue_invoice", okSuggestion.Str("issueType"));

        // Başarılı çalıştırma.
        var approvedOk = await (await PostAsync(client, $"/api/v1/revenue/suggestions/{okSuggestion.Str("id")}/decision", new { decision = "approve" })).ReadOkAsync();
        // Yanıt hemen döner; çalıştırma arka planda olduğundan o an "approved" veya (çok hızlıysa) "executed" olabilir.
        Assert.Contains(approvedOk.Str("status"), new[] { "approved", "executed" });
        var executed = await WaitForStatusAsync(client, okSuggestion.Str("id"), "executed");
        Assert.Equal(JsonValueKind.Null, executed.GetProperty("executionError").ValueKind);
        Assert.Contains("INV2-IT-0001", factory.PayPal.Reminders);

        // PayPal hatası: öneri failed olur ve hata kodu görünür.
        factory.PayPal.ReminderError = new ExternalServiceException("paypal_unavailable", "down");
        try
        {
            (await PostAsync(client, $"/api/v1/revenue/suggestions/{failingSuggestion.Str("id")}/decision", new { decision = "approve" })).EnsureSuccessStatusCode();
            var failed = await WaitForStatusAsync(client, failingSuggestion.Str("id"), "failed");
            Assert.Equal("paypal_unavailable", failed.Str("executionError"));
            Assert.DoesNotContain("INV2-IT-0002", factory.PayPal.Reminders);
        }
        finally
        {
            factory.PayPal.ReminderError = null;
        }
    }

    private async Task<(Guid Ok, Guid Failing)> AddOverduePayPalInvoicesAsync()
    {
        using var scope = factory.AppServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);

        var customer = Customer.Create(DataSource.Paypal, "PP-CUST-IT", "Integration Customer", "it@example.com", now);
        var ok = Invoice.Create(DataSource.Paypal, "INV2-IT-0001", "IT-0001", customer.Id, 99999m, "USD",
            InvoiceStatus.Sent, "SENT", today.AddDays(-20), now.AddDays(-40), now, now);
        var failing = Invoice.Create(DataSource.Paypal, "INV2-IT-0002", "IT-0002", customer.Id, 88888m, "USD",
            InvoiceStatus.Sent, "SENT", today.AddDays(-15), now.AddDays(-35), now, now);

        db.Customers.Add(customer);
        db.Invoices.AddRange(ok, failing);
        await db.SaveChangesAsync();

        return (ok.Id, failing.Id);
    }

    private static async Task<JsonElement> WaitForStatusAsync(HttpClient client, string suggestionId, string status)
    {
        var deadline = DateTime.UtcNow.AddSeconds(20);
        while (DateTime.UtcNow < deadline)
        {
            var list = await (await client.GetAsync($"/api/v1/revenue/suggestions?status={status}&pageSize=100")).ReadOkAsync();
            var match = list.Items().FirstOrDefault(s => s.Str("id") == suggestionId);
            if (match.ValueKind == JsonValueKind.Object)
            {
                return match;
            }

            await Task.Delay(500);
        }

        throw new Xunit.Sdk.XunitException($"Suggestion {suggestionId} did not reach status '{status}' within 20 seconds.");
    }

    // ---------- insight ----------

    [IntegrationFact]
    public async Task Insight_numbers_come_from_the_data_and_the_commentary_follows_the_requested_language()
    {
        var client = await factory.CreateAdminClientAsync();

        var insight = await (await PostAsync(client, "/api/v1/revenue/insights/generate?days=30", language: "de")).ReadOkAsync();

        Assert.Equal("de", insight.Str("commentaryLanguage"));
        Assert.False(string.IsNullOrWhiteSpace(insight.Str("commentary")));
        var metrics = insight.GetProperty("metrics");
        Assert.True(metrics.GetProperty("revenue").Amount() > 0);
        Assert.InRange(metrics.GetProperty("failureRate").GetDouble(), 0.0001, 1.0);
        Assert.True(metrics.GetProperty("overdueInvoiceCount").GetInt32() >= 5);
        Assert.NotEmpty(metrics.GetProperty("topFailureReasons").EnumerateArray());

        var list = await (await client.GetAsync("/api/v1/revenue/insights?pageSize=5")).ReadOkAsync();
        Assert.Equal(insight.Str("id"), list.Items().First().Str("id"));

        var invalid = await PostAsync(client, "/api/v1/revenue/insights/generate?days=500");
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
    }

    // ---------- AI Gateway ----------

    [IntegrationFact]
    public async Task The_cache_serves_exact_and_semantic_repeats_without_calling_the_provider_and_the_budget_blocks_new_calls()
    {
        var client = await factory.CreateAdminClientAsync();
        // Aynı etiket iki metinde de geçer: cache, sorgudaki sayıların aynı olmasını şart koşar (sipariş 123 / 456 karışmasın diye).
        var tag = Guid.NewGuid().ToString("N");
        var question = $"How do I reset my password? ({tag})";
        var paraphrase = $"I forgot my password, how can I reset it? ({tag})";
        factory.Embeddings.Vectors[question] = [1f, 0f, 0f, 0f];
        factory.Embeddings.Vectors[paraphrase] = [0.99f, 0.14f, 0f, 0f];

        var callsBefore = factory.Ai.Calls;
        var first = await CompleteAsync(client, question);
        var exact = await CompleteAsync(client, question);
        var semantic = await CompleteAsync(client, paraphrase);

        Assert.Equal("miss", first.GetProperty("cache").Str("status"));
        Assert.True(first.GetProperty("cost").Amount() > 0);
        Assert.Equal("hit", exact.GetProperty("cache").Str("status"));
        Assert.Equal("exact", exact.GetProperty("cache").Str("matchType"));
        Assert.Equal(0m, exact.GetProperty("cost").Amount());
        Assert.Equal("semantic", semantic.GetProperty("cache").Str("matchType"));
        Assert.True(semantic.GetProperty("cache").GetProperty("similarity").GetDouble() >= 0.93);
        Assert.Equal(callsBefore + 1, factory.Ai.Calls);   // yalnızca ilk istek sağlayıcıya gitti

        // cache: false her zaman sağlayıcıya gider.
        var bypass = await CompleteAsync(client, question, cache: false);
        Assert.Equal("bypass", bypass.GetProperty("cache").Str("status"));
        Assert.Equal(callsBefore + 2, factory.Ai.Calls);

        // İstatistikler ve kullanım özeti gerçek kayıtlardan gelir.
        var stats = await (await client.GetAsync("/api/v1/gateway/cache/stats")).ReadOkAsync();
        Assert.True(stats.GetProperty("hits").GetInt32() >= 2);
        var usage = await (await client.GetAsync($"/api/v1/gateway/usage/summary?{Range(7)}")).ReadOkAsync();
        Assert.True(usage.GetProperty("cache").GetProperty("hits").GetInt32() >= 2);
        Assert.True(usage.GetProperty("totalRequests").GetInt32() >= 2);

        // Bütçe: çok düşük limit yeni sağlayıcı çağrılarını engeller, cache'ten gelenler sunulmaya devam eder.
        try
        {
            (await PutAsync(client, "/api/v1/gateway/budgets", new { limit = new { amount = "0.000001", currency = "USD" }, enforceLimit = true })).EnsureSuccessStatusCode();

            var blocked = await PostAsync(client, "/api/v1/gateway/completions", new
            {
                messages = new[] { new { role = "user", content = $"A brand new question ({Guid.NewGuid():N})" } }
            });
            Assert.Equal((HttpStatusCode)422, blocked.StatusCode);
            Assert.Equal("budget_exceeded", (await blocked.ReadAsync()).Str("code"));

            var stillServed = await CompleteAsync(client, question);
            Assert.Equal("hit", stillServed.GetProperty("cache").Str("status"));

            var budget = await (await client.GetAsync("/api/v1/gateway/budgets")).ReadOkAsync();
            Assert.Equal("exceeded", budget.Str("level"));
        }
        finally
        {
            (await PutAsync(client, "/api/v1/gateway/budgets", new { limit = new { amount = "1000000.00", currency = "USD" }, enforceLimit = false }))
                .EnsureSuccessStatusCode();
        }

        var afterRestore = await CompleteAsync(client, $"Another new question ({Guid.NewGuid():N})");
        Assert.Equal("miss", afterRestore.GetProperty("cache").Str("status"));
    }

    [IntegrationFact]
    public async Task Prompt_optimization_removes_only_whitespace_noise_and_is_reported()
    {
        var client = await factory.CreateAdminClientAsync();

        var result = await (await PostAsync(client, "/api/v1/gateway/prompt/optimize", new
        {
            messages = new[] { new { role = "user", content = "Hello   \n\n\n\nWorld  " } }
        })).ReadOkAsync();

        Assert.Equal("Hello\n\nWorld", result.GetProperty("messages")[0].Str("content"));
        Assert.True(result.GetProperty("optimization").GetProperty("charactersSaved").GetInt32() > 0);
    }

    [IntegrationFact]
    public async Task Routing_rules_can_be_changed_with_enabled_models_only()
    {
        var client = await factory.CreateAdminClientAsync();
        var models = (await (await client.GetAsync("/api/v1/gateway/models")).ReadOkAsync()).EnumerateArray()
            .Where(m => m.GetProperty("isEnabled").GetBoolean()).Select(m => m.Str("modelId")).ToList();
        Assert.True(models.Count >= 2);

        var saved = await (await PutAsync(client, "/api/v1/gateway/routing-rules", new
        {
            taskType = "integration-test",
            primaryModelId = models[0],
            fallbackModelIds = new[] { models[1] }
        })).ReadOkAsync();
        Assert.Equal(models[0], saved.Str("primaryModelId"));

        var rules = (await (await client.GetAsync("/api/v1/gateway/routing-rules")).ReadOkAsync()).EnumerateArray().ToList();
        Assert.Equal("default", rules[0].Str("taskType"));
        Assert.Contains(rules, r => r.Str("taskType") == "integration-test");

        var unknown = await PutAsync(client, "/api/v1/gateway/routing-rules", new { taskType = "integration-test", primaryModelId = "no-such-model" });
        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
    }
}
