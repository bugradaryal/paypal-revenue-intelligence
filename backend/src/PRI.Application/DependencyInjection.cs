using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PRI.Application.AiGateway.Interfaces;
using PRI.Application.AiGateway.Services;
using PRI.Application.Common.Interfaces;
using PRI.Application.Common.Services;
using PRI.Application.Revenue.Interfaces;
using PRI.Application.Revenue.Services;
using PRI.Application.Common.Validators;

namespace PRI.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserManagementService, UserManagementService>();
        services.AddScoped<IPayPalSyncService, PayPalSyncService>();
        services.AddScoped<IPayPalWebhookService, PayPalWebhookService>();

        services.AddScoped<IAiProviderResolver, AiProviderResolver>();
        services.AddScoped<IModelRouter, ModelRouter>();
        services.AddScoped<IGatewayClientService, GatewayClientService>();
        services.AddScoped<IAiCompletionService, AiCompletionService>();
        services.AddSingleton<IPromptOptimizer, PromptOptimizer>();
        services.AddScoped<ISemanticCacheService, SemanticCacheService>();
        services.AddScoped<IBudgetService, BudgetService>();
        services.AddScoped<IModelCatalogService, ModelCatalogService>();
        services.AddScoped<IRoutingRuleService, RoutingRuleService>();
        services.AddScoped<IAiGateway, InProcessAiGateway>();

        services.AddScoped<IRevenueIssueSync, RevenueIssueSync>();
        services.AddScoped<IRationaleWriter, RationaleWriter>();
        services.AddScoped<ISuggestionService, SuggestionService>();
        services.AddScoped<IRecoveryActionExecutor, RecoveryActionExecutor>();
        services.AddScoped<IInsightService, InsightService>();

        return services;
    }
}
