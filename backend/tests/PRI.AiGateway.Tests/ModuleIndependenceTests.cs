using NetArchTest.Rules;
using PRI.Application.AiGateway.Interfaces;
using PRI.Domain.AiGateway.Enums;
using PRI.Infrastructure.AiGateway.Providers.OpenAi;

namespace PRI.AiGateway.Tests;

/// <summary>Mimari kural: AI Gateway, Revenue modülüne bağımlı olamaz (ileride bağımsız bir hizmet olarak sunulabilmesi için).</summary>
public class ModuleIndependenceTests
{
    private static readonly string[] RevenueNamespaces =
    [
        "PRI.Domain.Revenue",
        "PRI.Application.Revenue",
        "PRI.Infrastructure.Revenue"
    ];

    [Theory]
    [InlineData(typeof(AiProvider), "PRI.Domain.AiGateway")]
    [InlineData(typeof(IAiProvider), "PRI.Application.AiGateway")]
    [InlineData(typeof(OpenAiProvider), "PRI.Infrastructure.AiGateway")]
    public void Gateway_code_does_not_depend_on_the_revenue_module(Type anchorType, string gatewayNamespace)
    {
        var result = Types.InAssembly(anchorType.Assembly)
            .That().ResideInNamespaceStartingWith(gatewayNamespace)
            .ShouldNot().HaveDependencyOnAny(RevenueNamespaces)
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            "Revenue'ya bağımlı gateway tipleri: " + string.Join(", ", result.FailingTypeNames ?? []));
    }

    private static readonly string[] GatewayNamespaces =
    [
        "PRI.Domain.AiGateway",
        "PRI.Application.AiGateway",
        "PRI.Infrastructure.AiGateway"
    ];

    [Theory]
    [InlineData(typeof(PRI.Domain.Revenue.Entities.RevenueIssue), "PRI.Domain.Revenue")]
    [InlineData(typeof(PRI.Application.Revenue.Services.SuggestionService), "PRI.Application.Revenue")]
    [InlineData(typeof(PRI.Infrastructure.Revenue.PayPal.PayPalClient), "PRI.Infrastructure.Revenue")]
    public void Revenue_code_reaches_ai_only_through_the_common_IAiGateway_interface(Type anchorType, string revenueNamespace)
    {
        var result = Types.InAssembly(anchorType.Assembly)
            .That().ResideInNamespaceStartingWith(revenueNamespace)
            .ShouldNot().HaveDependencyOnAny(GatewayNamespaces)
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            "Gateway'e doğrudan bağımlı Revenue tipleri: " + string.Join(", ", result.FailingTypeNames ?? []));
    }
}
