namespace Incursa.Integrations.WorkOS.Access;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Incursa.Integrations.WorkOS.Abstractions.Authentication;
using Incursa.Integrations.WorkOS.Abstractions.Configuration;
using Incursa.Platform.Access;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

internal sealed class WorkOsTokenValidator : IWorkOsTokenValidator
{
    private const string CacheKeyPrefix = "workos-jwks:";

    private readonly HttpClient httpClient;
    private readonly IMemoryCache cache;
    private readonly WorkOsAuthOptions options;

    public WorkOsTokenValidator(
        HttpClient httpClient,
        IMemoryCache cache,
        IOptions<WorkOsAuthOptions> options)
    {
        this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        this.cache = cache ?? throw new ArgumentNullException(nameof(cache));
        ArgumentNullException.ThrowIfNull(options);
        this.options = options.Value;
    }

    public async Task<WorkOsTokenValidationResult> ValidateAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return new WorkOsTokenValidationResult(false, FailureCode: "missing_token", FailureMessage: "Access token is required.");
        }

        try
        {
            var signingKeys = await GetSigningKeysAsync(forceRefresh: false, cancellationToken).ConfigureAwait(false);
            return Validate(accessToken, signingKeys);
        }
        catch (SecurityTokenSignatureKeyNotFoundException)
        {
            try
            {
                var signingKeys = await GetSigningKeysAsync(forceRefresh: true, cancellationToken).ConfigureAwait(false);
                return Validate(accessToken, signingKeys);
            }
            catch (Exception ex) when (ex is SecurityTokenException or HttpRequestException)
            {
                return new WorkOsTokenValidationResult(false, FailureCode: "invalid_token", FailureMessage: ex.Message);
            }
        }
        catch (Exception ex) when (ex is SecurityTokenException or HttpRequestException)
        {
            return new WorkOsTokenValidationResult(false, FailureCode: "invalid_token", FailureMessage: ex.Message);
        }
    }

    private WorkOsTokenValidationResult Validate(string accessToken, IReadOnlyCollection<SecurityKey> signingKeys)
    {
        var validIssuer = options.GetIssuerUri().ToString().TrimEnd('/');
        var validAudiences = options.ExpectedAudiences
            .Where(static audience => !string.IsNullOrWhiteSpace(audience))
            .Select(static audience => audience.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (validAudiences.Length == 0 && !string.IsNullOrWhiteSpace(options.ClientId))
        {
            validAudiences = [options.ClientId.Trim()];
        }

        var tokenHandler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var principal = tokenHandler.ValidateToken(
            accessToken,
            new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = signingKeys,
                ValidateIssuer = true,
                ValidIssuers = [validIssuer, validIssuer + "/"],
                ValidateAudience = validAudiences.Length > 0,
                ValidAudiences = validAudiences,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1),
                NameClaimType = "sub",
                RoleClaimType = AccessClaimTypes.Role,
            },
            out _);

        if (!WorkOsTokenClaimsParser.TryParse(accessToken, out var claims) || claims is null)
        {
            return new WorkOsTokenValidationResult(false, FailureCode: "invalid_token", FailureMessage: "Unable to parse WorkOS token claims.");
        }

        ClaimsIdentity identity = new(principal.Claims, "WorkOS");
        AddNormalizedClaims(identity, claims);

        return new WorkOsTokenValidationResult(true, claims, new ClaimsPrincipal(identity));
    }

    private async Task<IReadOnlyCollection<SecurityKey>> GetSigningKeysAsync(bool forceRefresh, CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeyPrefix + options.ClientId;
        if (forceRefresh)
        {
            cache.Remove(cacheKey);
        }

        return await cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = options.JwksCacheDuration;
            using var response = await httpClient.GetAsync(options.GetJwksUri(), cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var jwks = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var keySet = new JsonWebKeySet(jwks);
            return (IReadOnlyCollection<SecurityKey>)keySet.GetSigningKeys().ToArray();
        }).ConfigureAwait(false) ?? Array.Empty<SecurityKey>();
    }

    private static void AddNormalizedClaims(ClaimsIdentity identity, WorkOsTokenClaims claims)
    {
        AddClaimIfMissing(identity, AccessClaimTypes.SessionId, claims.SessionId);
        AddClaimIfMissing(identity, AccessClaimTypes.OrganizationId, claims.OrganizationId);
        AddClaimIfMissing(identity, "sid", claims.SessionId);
        AddClaimIfMissing(identity, "org_id", claims.OrganizationId);

        foreach (var role in claims.Roles)
        {
            AddClaimIfMissing(identity, AccessClaimTypes.Role, role);
            AddClaimIfMissing(identity, ClaimTypes.Role, role);
        }

        foreach (var permission in claims.Permissions)
        {
            AddClaimIfMissing(identity, AccessClaimTypes.Permission, permission);
        }

        foreach (var featureFlag in claims.FeatureFlags)
        {
            AddClaimIfMissing(identity, AccessClaimTypes.FeatureFlag, featureFlag);
        }

        foreach (var entitlement in claims.Entitlements)
        {
            AddClaimIfMissing(identity, AccessClaimTypes.Entitlement, entitlement);
        }
    }

    private static void AddClaimIfMissing(ClaimsIdentity identity, string claimType, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value) && !identity.HasClaim(claimType, value))
        {
            identity.AddClaim(new Claim(claimType, value));
        }
    }
}
