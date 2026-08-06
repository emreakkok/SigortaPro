using System.Diagnostics;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Infrastructure.Security;
using SigortaPro.WebAPI.HealthChecks;
using SigortaPro.WebAPI.Services;

namespace SigortaPro.WebAPI.Extensions;

// WebAPI composition root yardımcıları.
public static class ServiceCollectionExtensions
{
    // Health check "ready" etiketi (CA1861: tekrar eden inline dizi yerine tek örnek).
    private static readonly string[] ReadyTag = { "ready" };


    public static IServiceCollection AddWebApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        services.AddJwtAuthentication(configuration);
        services.AddAuthorization();

        services.AddConsistentModelValidationResponse();
        services.AddCorsPolicy(configuration);
        services.AddSwaggerDocumentation();
        services.AddAuthRateLimiting();
        services.AddHealthCheckServices();

        return services;
    }

    // [ApiController] otomatik model doğrulama hatalarını, ExceptionHandlingMiddleware ile aynı RFC 7807
    // formatına getirir; böylece model-binding ve iş kuralı doğrulamaları tutarlı yanıt verir.
    private static IServiceCollection AddConsistentModelValidationResponse(this IServiceCollection services)
    {
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var problemDetails = new ValidationProblemDetails(context.ModelState)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Doğrulama hatası",
                    Type = WebApiConstants.ErrorTypeBaseUri + "validation",
                    Detail = "Bir veya daha fazla doğrulama hatası oluştu.",
                };

                var httpContext = context.HttpContext;
                problemDetails.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;
                if (httpContext.Items.TryGetValue(WebApiConstants.CorrelationIdItemKey, out var correlationId)
                    && correlationId is not null)
                {
                    problemDetails.Extensions["correlationId"] = correlationId.ToString();
                }

                return new ObjectResult(problemDetails)
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    ContentTypes = { "application/problem+json" },
                };
            };
        });

        return services;
    }

    private static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
            ?? throw new InvalidOperationException("'JwtSettings' yapılandırması appsettings.json içinde tanımlı değil.");

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey));

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                // Claim tiplerini olduğu gibi koru (sub → NameIdentifier yeniden eşlemesi yapılmasın).
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    NameClaimType = ClaimTypes.NameIdentifier,
                    RoleClaimType = ClaimTypes.Role,
                };

                // SignalR WebSocket bağlantıları Authorization header taşıyamaz; hub yoluna gelen
                // isteklerde token, SignalR istemcisinin standart "access_token" query parametresinden okunur.
                // Yalnızca hub path'i ile sınırlıdır — normal API uçlarında davranış değişmez.
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        if (!string.IsNullOrEmpty(accessToken)
                            && context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    },
                };
            });

        return services;
    }

    // /: React SPA origin'lerine kısıtlı CORS.
    private static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        if (allowedOrigins is null || allowedOrigins.Length == 0)
        {
            // Yapılandırma yoksa Vite dev sunucusunun varsayılan origin'ine düşülür.
            allowedOrigins = new[] { "http://localhost:5173" };
        }

        services.AddCors(options =>
        {
            options.AddPolicy(WebApiConstants.CorsPolicyName, policy =>
                policy.WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()
                    .WithExposedHeaders(WebApiConstants.CorrelationIdHeader));
        });

        return services;
    }

    // Swagger/OpenAPI + JWT "Authorize" desteği.
    private static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "SigortaPro API",
                Version = "v1",
                Description = "Tek acenteli, B2C sigorta poliçe yönetim sistemi API'si.",
            });

            var jwtSecurityScheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "JWT Bearer token. Örnek: \"Bearer {token}\"",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = JwtBearerDefaults.AuthenticationScheme,
                BearerFormat = "JWT",
                Reference = new OpenApiReference
                {
                    Id = JwtBearerDefaults.AuthenticationScheme,
                    Type = ReferenceType.SecurityScheme,
                },
            };

            options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, jwtSecurityScheme);
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                { jwtSecurityScheme, Array.Empty<string>() },
            });

            // XML doc yorumlarını (varsa) Swagger'a dahil et.
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }
        });

        return services;
    }

    // : Login uçlarına rate limiting (built-in ASP.NET Core 8 rate limiter).
    private static IServiceCollection AddAuthRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Dakikada IP başına 10 istek; brute-force denemelerini yavaşlatır.
            options.AddPolicy(WebApiConstants.AuthRateLimitPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    }));
        });

        return services;
    }

    private static IServiceCollection AddHealthCheckServices(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database", tags: ReadyTag);

        return services;
    }
}
