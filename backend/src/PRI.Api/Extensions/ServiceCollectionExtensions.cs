using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PRI.Api.Authentication;
using PRI.Api.Configuration;
using PRI.Api.Filters;
using PRI.Api.Middleware;
using PRI.Infrastructure.Auth;
using PRI.Shared.Constants;

namespace PRI.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddControllers(options =>
            {
                // Doğrulamayı FluentValidation yapar; MVC'nin otomatik "required" kuralı kapatılır.
                options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
                options.Filters.Add<ValidationFilter>();
            })
            .AddJsonOptions(options =>
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower)));

        services.AddEndpointsApiExplorer();
        services.AddSwaggerWithBearer();

        services.AddProblemDetails(options =>
            options.CustomizeProblemDetails = context =>
            {
                var problem = context.ProblemDetails;
                problem.Extensions["traceId"] = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
                // Framework'ün ürettiği hatalarda (404, 405, 401...) da "code" alanı bulunsun.
                problem.Extensions.TryAdd("code", ErrorCodes.FromStatusCode(problem.Status));
            });
        services.AddExceptionHandler<GlobalExceptionHandler>();

        services.AddHealthChecks();
        services.AddSingleton(TimeProvider.System);

        services.AddSingleton<PRI.Api.BackgroundJobs.RecoveryActionQueue>();
        services.AddSingleton<PRI.Application.Revenue.Interfaces.IRecoveryActionQueue>(
            sp => sp.GetRequiredService<PRI.Api.BackgroundJobs.RecoveryActionQueue>());
        services.AddHostedService<PRI.Api.BackgroundJobs.RecoveryActionWorker>();
        services.AddFrontendCors(configuration);
        services.AddJwtAuthentication(configuration);

        return services;
    }

    private static IServiceCollection AddSwaggerWithBearer(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "PayPal Revenue Intelligence API", Version = "v1" });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "POST /api/v1/auth/login ile alınan access token."
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                }] = Array.Empty<string>()
            });

            // Gateway uçları alternatif olarak API anahtarı kabul eder (üçüncü taraf istemciler).
            options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Name = ApiKeyDefaults.HeaderName,
                Description = "POST /api/v1/gateway/clients ile üretilen API anahtarı (yalnızca /gateway/completions için)."
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "ApiKey" }
                }] = Array.Empty<string>()
            });
        });

        return services;
    }

    private static IServiceCollection AddFrontendCors(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection(CorsSettings.SectionName).Get<CorsSettings>() ?? new CorsSettings();

        services.AddCors(options =>
            options.AddPolicy(CorsSettings.PolicyName, policy => policy
                .WithOrigins(settings.AllowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()));

        return services;
    }

    private static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwt = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();

        if (jwt.Key.Length < 32)
        {
            throw new InvalidOperationException("Jwt:Key must be configured and at least 32 characters long.");
        }

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                    NameClaimType = AuthClaims.UserId,
                    RoleClaimType = AuthClaims.Role
                };
            })
            .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(ApiKeyDefaults.Scheme, null);

        services.AddAuthorization();

        return services;
    }
}
