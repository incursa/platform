namespace Incursa.Integrations.WorkOS.Tests;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Incursa.Integrations.WorkOS.Abstractions.Authentication;
using Incursa.Integrations.WorkOS.Abstractions.Configuration;
using Incursa.Integrations.WorkOS.Access;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

[TestClass]
public sealed class WorkOsAuthenticationClientTests
{
    [TestMethod]
    public async Task RefreshAsync_CustomAuthDomain_SendsRefreshRequestAndMapsRotatedTokensAsync()
    {
        var options = CreateOptions();
        var accessToken = CreateUnsignedAccessToken("user_123");
        HttpRequestMessage? capturedRequest = null;
        var client = new WorkOsAuthenticationClient(
            new HttpClient(new StubHandler(async request =>
            {
                capturedRequest = request;
                var formBody = await request.Content!.ReadAsStringAsync().ConfigureAwait(false);

                Assert.AreEqual(new Uri("https://auth.example.test/user_management/authenticate"), request.RequestUri);
                Assert.IsTrue(formBody.Contains("grant_type=refresh_token", StringComparison.Ordinal));
                Assert.IsTrue(formBody.Contains("refresh_token=refresh-token-original", StringComparison.Ordinal));
                Assert.IsTrue(formBody.Contains("organization_id=org_456", StringComparison.Ordinal));
                Assert.IsTrue(formBody.Contains("client_id=client_123", StringComparison.Ordinal));

                return JsonResponse(
                    """
                    {
                      "user": {
                        "id": "user_123",
                        "email": "ada@example.com",
                        "first_name": "Ada",
                        "last_name": "Lovelace",
                        "email_verified": true
                      },
                      "organization_id": "org_456",
                      "access_token": "__ACCESS_TOKEN__",
                      "refresh_token": "refresh-token-rotated"
                    }
                    """.Replace("__ACCESS_TOKEN__", accessToken, StringComparison.Ordinal));
            })),
            Options.Create(options));

        var result = await client.RefreshAsync(
            new WorkOsRefreshRequest("refresh-token-original", "org_456"),
            CancellationToken.None);

        Assert.IsNotNull(capturedRequest);
        var success = result as WorkOsAuthenticationSuccess;
        Assert.IsNotNull(success);
        Assert.AreEqual("refresh-token-rotated", success.Session.RefreshToken);
        Assert.AreEqual("org_456", success.Session.Claims.OrganizationId);
        Assert.AreEqual("ada@example.com", success.Session.Email);
        CollectionAssert.AreEquivalent(new[] { "member" }, success.Session.Claims.Roles.ToArray());
    }

    [TestMethod]
    public async Task SignOutAsync_SessionId_RevokesSessionAndBuildsLogoutUrlAsync()
    {
        var options = CreateOptions();
        HttpRequestMessage? capturedRequest = null;
        var client = new WorkOsAuthenticationClient(
            new HttpClient(new StubHandler(request =>
            {
                capturedRequest = request;
                Assert.AreEqual(new Uri("https://api.example.test/user_management/sessions/session_123/revoke"), request.RequestUri);
                Assert.IsNotNull(request.Headers.Authorization);
                Assert.AreEqual("Bearer", request.Headers.Authorization.Scheme);
                Assert.AreEqual("api_key_test", request.Headers.Authorization.Parameter);
                return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.NoContent));
            })),
            Options.Create(options));

        var result = await client.SignOutAsync(
            new WorkOsSignOutRequest("session_123", "https://app.example.test/signed-out"),
            CancellationToken.None);

        Assert.IsNotNull(capturedRequest);
        Assert.IsTrue(result.SessionRevoked);
        Assert.AreEqual(
            new Uri("https://auth.example.test/user_management/sessions/logout?session_id=session_123&return_to=https%3A%2F%2Fapp.example.test%2Fsigned-out"),
            result.LogoutUrl);
    }

    [TestMethod]
    public async Task ValidateAsync_CustomIssuerAndCachedJwks_ReturnsNormalizedClaimsAsync()
    {
        var options = CreateOptions();
        using var rsa = RSA.Create(2048);
        var signingKey = new RsaSecurityKey(rsa.ExportParameters(true))
        {
            KeyId = "kid-123",
        };
        var token = CreateSignedAccessToken(signingKey, "https://auth.example.test/", "client_123");
        var jwks = CreateJwks(signingKey);
        var requestCount = 0;

        var validator = new WorkOsTokenValidator(
            new HttpClient(new StubHandler(request =>
            {
                requestCount++;
                Assert.AreEqual(options.GetJwksUri(), request.RequestUri);
                return Task.FromResult(JsonResponse(jwks));
            })),
            new MemoryCache(Options.Create(new MemoryCacheOptions())),
            Options.Create(options));

        var first = await validator.ValidateAsync(token, CancellationToken.None);
        var second = await validator.ValidateAsync(token, CancellationToken.None);

        Assert.IsTrue(first.IsValid);
        Assert.IsTrue(second.IsValid);
        Assert.AreEqual(1, requestCount);
        Assert.IsNotNull(first.Claims);
        Assert.AreEqual("user_123", first.Claims.SubjectId);
        Assert.AreEqual("session_123", first.Claims.SessionId);
        Assert.AreEqual("org_456", first.Claims.OrganizationId);
        CollectionAssert.AreEquivalent(new[] { "admin", "owner" }, first.Claims.Roles.ToArray());
        CollectionAssert.AreEquivalent(new[] { "audit:read", "users:write" }, first.Claims.Permissions.ToArray());
        CollectionAssert.AreEquivalent(new[] { "beta-dashboard" }, first.Claims.FeatureFlags.ToArray());
        CollectionAssert.AreEquivalent(new[] { "tier:pro" }, first.Claims.Entitlements.ToArray());
        Assert.IsNotNull(first.Principal);
        CollectionAssert.AreEquivalent(
            new[] { "admin", "owner" },
            first.Principal.FindAll(Incursa.Platform.Access.AccessClaimTypes.Role).Select(static claim => claim.Value).ToArray());
    }

    [TestMethod]
    public void WorkOsAuthOptionsValidation_InvalidOptions_ReturnsAllFailures()
    {
        var validation = new WorkOsAuthOptionsValidation().Validate(
            null,
            new WorkOsAuthOptions
            {
                ApiBaseUrl = "not-a-uri",
                AuthApiBaseUrl = "still-not-a-uri",
                Issuer = "also-not-a-uri",
                ClientId = "",
                ClientSecret = "",
                ApiKey = "",
                RequestTimeout = TimeSpan.Zero,
                JwksCacheDuration = TimeSpan.Zero,
            });

        Assert.IsFalse(validation.Succeeded);
        var failures = validation.Failures?.ToList() ?? throw new AssertFailedException("Expected validation failures.");
        CollectionAssert.Contains(failures, "ApiBaseUrl must be an absolute URI.");
        CollectionAssert.Contains(failures, "AuthApiBaseUrl must be an absolute URI when provided.");
        CollectionAssert.Contains(failures, "Issuer must be an absolute URI when provided.");
        CollectionAssert.Contains(failures, "ClientId is required.");
        CollectionAssert.Contains(failures, "ApiKey or ClientSecret is required.");
        CollectionAssert.Contains(failures, "RequestTimeout must be greater than zero.");
        CollectionAssert.Contains(failures, "JwksCacheDuration must be greater than zero.");
    }

    private static WorkOsAuthOptions CreateOptions() =>
        new()
        {
            ApiBaseUrl = "https://api.example.test",
            AuthApiBaseUrl = "https://auth.example.test",
            Issuer = "https://auth.example.test",
            ClientId = "client_123",
            ClientSecret = "client_secret_test",
            ApiKey = "api_key_test",
            ExpectedAudiences = ["client_123"],
        };

    private static HttpResponseMessage JsonResponse(string payload) =>
        new(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json"),
        };

    private static string CreateUnsignedAccessToken(string subject)
    {
        var token = new JwtSecurityToken(
            claims:
            [
                new System.Security.Claims.Claim("sub", subject),
                new System.Security.Claims.Claim("roles", "[\"member\"]"),
            ]);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string CreateSignedAccessToken(SecurityKey signingKey, string issuer, string audience)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            Claims = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["sub"] = "user_123",
                ["sid"] = "session_123",
                ["org_id"] = "org_456",
                ["roles"] = new[] { "admin", "owner" },
                ["permissions"] = new[] { "audit:read", "users:write" },
                ["feature_flags"] = new[] { "beta-dashboard" },
                ["entitlements"] = new[] { "tier:pro" },
            },
            NotBefore = DateTime.UtcNow.AddMinutes(-1),
            Expires = DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.RsaSha256),
        };

        return new JwtSecurityTokenHandler().CreateEncodedJwt(descriptor);
    }

    private static string CreateJwks(RsaSecurityKey signingKey)
    {
        var key = JsonWebKeyConverter.ConvertFromRSASecurityKey(signingKey);
        return JsonSerializer.Serialize(new { keys = new[] { key } });
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> handler;

        public StubHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
        {
            this.handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            handler(request);
    }
}
