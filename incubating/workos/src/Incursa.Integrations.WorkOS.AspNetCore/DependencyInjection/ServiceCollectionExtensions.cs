namespace Incursa.Integrations.WorkOS.AspNetCore.DependencyInjection;

using System.Security.Claims;
using Incursa.Integrations.WorkOS.Abstractions.Claims;
using Incursa.Integrations.WorkOS.Abstractions.Configuration;
using Incursa.Integrations.WorkOS.AspNetCore.Auth;
using Incursa.Integrations.WorkOS.AspNetCore.Webhooks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWorkOsAspNetCore(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<WorkOsRuntimeAuthMiddleware>();
        services.AddScoped<WorkOsWebhookEndpoint>();
        services.AddScoped<WorkOsUserProfileRefreshMiddleware>();
        services.AddScoped<WorkOsRequestAuthContextAccessor>();
        services.AddScoped<IWorkOsRequestAuthContextAccessor>(static sp => sp.GetRequiredService<WorkOsRequestAuthContextAccessor>());
        services.AddScoped<IWorkOsRequestAuthContextSetter>(static sp => sp.GetRequiredService<WorkOsRequestAuthContextAccessor>());
        return services;
    }

    public static IServiceCollection AddWorkOsOidcAuthKit(
        this IServiceCollection services,
        Action<WorkOsOidcOptions> configureOptions,
        string openIdConnectScheme = "workos",
        string cookieScheme = CookieAuthenticationDefaults.AuthenticationScheme)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        var options = new WorkOsOidcOptions();
        configureOptions(options);
        services.AddSingleton(options);

        var auth = services.AddAuthentication();

        auth.AddCookie(cookieScheme);
        auth.AddOpenIdConnect(openIdConnectScheme, oidc =>
        {
            oidc.SignInScheme = cookieScheme;
            oidc.Authority = options.Authority;
            oidc.ClientId = options.ClientId;
            oidc.ClientSecret = options.ClientSecret;
            oidc.CallbackPath = options.CallbackPath;
            oidc.SignedOutCallbackPath = options.SignedOutCallbackPath;
            oidc.ResponseType = "code";
            oidc.RequireHttpsMetadata = options.RequireHttpsMetadata;
            oidc.SaveTokens = false;
            oidc.GetClaimsFromUserInfoEndpoint = true;
            oidc.Scope.Clear();

            foreach (var scope in options.Scopes.Where(static x => !string.IsNullOrWhiteSpace(x)))
            {
                oidc.Scope.Add(scope.Trim());
            }

            oidc.Events ??= new OpenIdConnectEvents();
            var previous = oidc.Events.OnTicketReceived;
            oidc.Events.OnTicketReceived = async context =>
            {
                try
                {
                    if (context.Principal?.Identity is ClaimsIdentity identity)
                    {
                        var accessToken = context.Properties?.GetTokenValue("access_token");
                        if (!string.IsNullOrWhiteSpace(accessToken))
                        {
                            WorkOsAccessTokenClaims.TryAddClaimsFromAccessToken(accessToken, identity);
                        }

                        var enricher = context.HttpContext.RequestServices.GetService<IWorkOsClaimsEnricher>();
                        var hydrationOptions = context.HttpContext.RequestServices.GetService<WorkOsUserProfileHydrationOptions>();
                        var hydrateOnSignIn = hydrationOptions?.Enabled == true && hydrationOptions.HydrateOnSignIn;
                        if (hydrateOnSignIn && enricher is not null && context.Principal is not null)
                        {
                            await enricher.EnrichAsync(context.Principal, identity, context.HttpContext.RequestAborted).ConfigureAwait(false);
                        }
                    }
                }
                catch (Exception ex)
                {
                    var loggerFactory = context.HttpContext.RequestServices.GetService<ILoggerFactory>();
                    var logger = loggerFactory?.CreateLogger("Incursa.Integrations.WorkOS.AspNetCore") ?? NullLogger.Instance;
                    logger.LogWarning(ex, "Failed to enrich principal with WorkOS claims.");
                }

                if (previous is not null)
                {
                    await previous(context).ConfigureAwait(false);
                }
            };
        });

        return services;
    }

    public static IApplicationBuilder UseWorkOsRuntimeAuth(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<WorkOsRuntimeAuthMiddleware>();
    }

    public static IApplicationBuilder UseWorkOsWebhookEndpoint(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<WorkOsWebhookEndpoint>();
    }

    public static IApplicationBuilder UseWorkOsUserProfileRefresh(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<WorkOsUserProfileRefreshMiddleware>();
    }

    public static IApplicationBuilder UseWorkOsWidgetNoStoreResponses(
        this IApplicationBuilder app,
        PathString widgetsPathPrefix = default)
    {
        ArgumentNullException.ThrowIfNull(app);

        var effectivePrefix = widgetsPathPrefix == default ? new PathString("/account") : widgetsPathPrefix;

        return app.Use(async (context, next) =>
        {
            await next().ConfigureAwait(false);

            if (!HttpMethods.IsGet(context.Request.Method))
            {
                return;
            }

            if (!context.Request.Path.StartsWithSegments(effectivePrefix, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (!IsHtmlResponse(context.Response))
            {
                return;
            }

            var headers = context.Response.Headers;
            headers["Cache-Control"] = "no-store, no-cache, max-age=0, must-revalidate";
            headers["Pragma"] = "no-cache";
            headers["Expires"] = "0";
        });
    }

    private static bool IsHtmlResponse(HttpResponse response)
    {
        if (string.IsNullOrWhiteSpace(response.ContentType))
        {
            return true;
        }

        return response.ContentType.StartsWith("text/html", StringComparison.OrdinalIgnoreCase);
    }
}

