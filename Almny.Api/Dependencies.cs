using System.Text;
using System.Threading.RateLimiting;
using Almny.Api.Authentication;
using Almny.Api.Authentication.Filters;
using Almny.Api.Configurations;
using Almny.Api.Entities;
using Almny.Api.Persistence;
using Almny.Api.Services;
using FluentValidation;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

namespace Almny.Api;

public static class Dependencies
{
    public static IServiceCollection AddDependency(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedEmail = true;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        services.AddHttpContextAccessor();

        services.AddSingleton<IJwtProvider, JwtProvider>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IEmailService, EmailService>();

        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        services.AddControllers(options =>
        {
            options.Filters.Add<ValidationFilter>();
        });

        services.Configure<MailConfig>(configuration.GetSection(MailConfig.SectionName));

        return services;
    }

    public static IServiceCollection AddAuthConfig(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSection = configuration.GetSection(JwtOptions.SectionName);
        services.Configure<JwtOptions>(jwtSection);

        var key = Encoding.UTF8.GetBytes(jwtSection["Key"]
            ?? throw new InvalidOperationException("JWT Key is not configured."));

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.MapInboundClaims = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSection["Issuer"],
                ValidAudience = jwtSection["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(key)
            };
        });

        services.AddAuthorization();

        return services;
    }

    public static IServiceCollection AddRateLimitingConfig(this IServiceCollection services, IConfiguration configuration)
    {
        var rateLimitingOptions = new RateLimitingOptions();
        configuration.GetSection(RateLimitingOptions.SectionName).Bind(rateLimitingOptions);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.ContentType = "application/json";

                var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfterValue)
                    ? retryAfterValue.TotalSeconds
                    : rateLimitingOptions.Global.Window;

                context.HttpContext.Response.Headers.RetryAfter = retryAfter.ToString();

                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    error = "Too many requests. Please try again later.",
                    retryAfterSeconds = retryAfter
                }, cancellationToken);
            };

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimitingOptions.Global.PermitLimit,
                        Window = TimeSpan.FromSeconds(rateLimitingOptions.Global.Window),
                        QueueLimit = rateLimitingOptions.Global.QueueLimit
                    }));

            options.AddFixedWindowLimiter("Authentication", limiterOptions =>
            {
                limiterOptions.PermitLimit = rateLimitingOptions.Authentication.PermitLimit;
                limiterOptions.Window = TimeSpan.FromSeconds(rateLimitingOptions.Authentication.Window);
                limiterOptions.QueueLimit = rateLimitingOptions.Authentication.QueueLimit;
            });

            options.AddFixedWindowLimiter("Api", limiterOptions =>
            {
                limiterOptions.PermitLimit = rateLimitingOptions.Api.PermitLimit;
                limiterOptions.Window = TimeSpan.FromSeconds(rateLimitingOptions.Api.Window);
                limiterOptions.QueueLimit = rateLimitingOptions.Api.QueueLimit;
            });
        });

        return services;
    }

    public static IServiceCollection AddFluentValidation(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(Dependencies).Assembly);
        return services;
    }

    public static IServiceCollection AddMapsterDependency(this IServiceCollection services)
    {
        var config = TypeAdapterConfig.GlobalSettings;
        config.Scan(typeof(Dependencies).Assembly);

        services.AddSingleton(config);
        services.AddScoped<IMapper, ServiceMapper>();

        return services;
    }

    public static IServiceCollection AddSwaggerConfig(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Almny API",
                Version = "v1",
                Description = "Almny Platform API with JWT Authentication"
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "Enter 'Bearer {token}'",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT"
            });

            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecuritySchemeReference("Bearer"),
                    new List<string>()
                }
            });

            options.CustomSchemaIds(type => type.FullName);

            var xmlFile = $"{typeof(Dependencies).Assembly.GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
                options.IncludeXmlComments(xmlPath);
        });

        return services;
    }
}
