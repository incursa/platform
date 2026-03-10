namespace Incursa.Integrations.WorkOS.Access;

using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Incursa.Integrations.WorkOS.Abstractions.Authentication;
using Incursa.Integrations.WorkOS.Abstractions.Configuration;
using Microsoft.Extensions.Options;

internal sealed class WorkOsAuthenticationClient :
    IWorkOsAuthenticationClient,
    IWorkOsMagicAuthClient,
    IWorkOsSessionClient
{
    private readonly HttpClient httpClient;
    private readonly WorkOsAuthOptions options;

    public WorkOsAuthenticationClient(HttpClient httpClient, IOptions<WorkOsAuthOptions> options)
    {
        this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        ArgumentNullException.ThrowIfNull(options);
        this.options = options.Value;
    }

    public Task<Uri> CreateAuthorizationUrlAsync(
        WorkOsAuthorizationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var parameters = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["client_id"] = options.ClientId.Trim(),
            ["redirect_uri"] = request.RedirectUri,
            ["response_type"] = "code",
            ["scope"] = BuildScope(request),
        };

        Add(parameters, "state", request.State);
        Add(parameters, "provider", request.Provider);
        Add(parameters, "connection_id", request.ConnectionId);
        Add(parameters, "organization_id", request.OrganizationId);
        Add(parameters, "domain_hint", request.DomainHint);
        Add(parameters, "login_hint", request.LoginHint);
        Add(parameters, "screen_hint", request.ScreenHint);
        Add(parameters, "code_challenge", request.CodeChallenge);
        Add(parameters, "code_challenge_method", request.CodeChallengeMethod);
        Add(parameters, "provider_scope", request.ProviderScopes is { Count: > 0 } ? string.Join(' ', request.ProviderScopes) : null);

        if (request.AdditionalParameters is not null)
        {
            foreach (var pair in request.AdditionalParameters)
            {
                if (!string.IsNullOrWhiteSpace(pair.Key) && !string.IsNullOrWhiteSpace(pair.Value))
                {
                    parameters[pair.Key.Trim()] = pair.Value.Trim();
                }
            }
        }

        var builder = new UriBuilder(new Uri(options.GetAuthApiBaseUri(), options.AuthorizationPath))
        {
            Query = ToQueryString(parameters),
        };

        return Task.FromResult(builder.Uri);
    }

    public Task<WorkOsAuthenticationResult> ExchangeCodeAsync(
        WorkOsCodeExchangeRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        Dictionary<string, string> form = new(StringComparer.Ordinal)
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = options.ClientId.Trim(),
            ["client_secret"] = options.GetEffectiveClientSecret(),
            ["code"] = request.Code,
        };

        Add(form, "redirect_uri", request.RedirectUri);
        Add(form, "code_verifier", request.CodeVerifier);
        Add(form, "invitation_token", request.InvitationToken);
        AddMetadata(form, request.Metadata);
        return AuthenticateAsync(form, cancellationToken);
    }

    public Task<WorkOsAuthenticationResult> AuthenticateWithPasswordAsync(
        WorkOsPasswordAuthenticationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        Dictionary<string, string> form = new(StringComparer.Ordinal)
        {
            ["grant_type"] = "password",
            ["client_id"] = options.ClientId.Trim(),
            ["client_secret"] = options.GetEffectiveClientSecret(),
            ["email"] = request.Email,
            ["password"] = request.Password,
        };

        AddMetadata(form, request.Metadata);
        return AuthenticateAsync(form, cancellationToken);
    }

    public async Task<WorkOsTotpEnrollment> EnrollTotpAsync(
        WorkOsTotpEnrollmentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var message = CreateJsonRequest(
            HttpMethod.Post,
            new Uri(options.GetApiBaseUri(), options.TotpEnrollPath),
            new EnrollTotpFactorDto
            {
                Type = "totp",
                Issuer = string.IsNullOrWhiteSpace(request.Issuer) ? options.GetIssuerUri().Host : request.Issuer.Trim(),
                User = request.User,
            },
            includeApiKey: true);

        using var response = await httpClient.SendAsync(message, cancellationToken).ConfigureAwait(false);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var factor = Deserialize<FactorDto>(payload);
        return new WorkOsTotpEnrollment(
            factor.Id ?? string.Empty,
            factor.Totp?.Issuer ?? request.Issuer,
            factor.Totp?.User ?? request.User,
            factor.Totp?.QrCode,
            factor.Totp?.Secret,
            factor.Totp?.Uri);
    }

    public Task<WorkOsAuthenticationResult> CompleteMagicAuthAsync(
        WorkOsMagicAuthCompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        Dictionary<string, string> form = new(StringComparer.Ordinal)
        {
            ["grant_type"] = "urn:workos:oauth:grant-type:magic-auth:code",
            ["client_id"] = options.ClientId.Trim(),
            ["client_secret"] = options.GetEffectiveClientSecret(),
            ["code"] = request.Code,
        };

        AddMetadata(form, request.Metadata);
        return AuthenticateAsync(form, cancellationToken);
    }

    public Task<WorkOsAuthenticationResult> CompleteEmailVerificationAsync(
        WorkOsEmailVerificationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        Dictionary<string, string> form = new(StringComparer.Ordinal)
        {
            ["grant_type"] = "urn:workos:oauth:grant-type:email-verification:code",
            ["client_id"] = options.ClientId.Trim(),
            ["client_secret"] = options.GetEffectiveClientSecret(),
            ["pending_authentication_token"] = request.PendingAuthenticationToken,
            ["code"] = request.Code,
        };

        Add(form, "email_verification_id", request.EmailVerificationId);
        AddMetadata(form, request.Metadata);
        return AuthenticateAsync(form, cancellationToken);
    }

    public async Task<WorkOsAuthenticationResult> CompleteTotpAsync(
        WorkOsTotpAuthenticationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            throw new ArgumentException("Code is required.", nameof(request));
        }

        var challengeId = request.AuthenticationChallengeId;
        if (string.IsNullOrWhiteSpace(challengeId))
        {
            if (string.IsNullOrWhiteSpace(request.AuthenticationFactorId))
            {
                throw new ArgumentException(
                    "An authentication factor id or authentication challenge id is required.",
                    nameof(request));
            }

            challengeId = await CreateTotpChallengeAsync(request.AuthenticationFactorId, cancellationToken).ConfigureAwait(false);
        }

        Dictionary<string, string> form = new(StringComparer.Ordinal)
        {
            ["grant_type"] = "urn:workos:oauth:grant-type:mfa-totp",
            ["client_id"] = options.ClientId.Trim(),
            ["client_secret"] = options.GetEffectiveClientSecret(),
            ["pending_authentication_token"] = request.PendingAuthenticationToken,
            ["authentication_challenge_id"] = challengeId,
            ["code"] = request.Code,
        };

        AddMetadata(form, request.Metadata);
        return await AuthenticateAsync(form, cancellationToken).ConfigureAwait(false);
    }

    public Task<WorkOsAuthenticationResult> CompleteOrganizationSelectionAsync(
        WorkOsOrganizationSelectionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        Dictionary<string, string> form = new(StringComparer.Ordinal)
        {
            ["grant_type"] = "urn:workos:oauth:grant-type:organization-selection",
            ["client_id"] = options.ClientId.Trim(),
            ["client_secret"] = options.GetEffectiveClientSecret(),
            ["pending_authentication_token"] = request.PendingAuthenticationToken,
            ["organization_id"] = request.OrganizationId,
        };

        AddMetadata(form, request.Metadata);
        return AuthenticateAsync(form, cancellationToken);
    }

    public Task<WorkOsAuthenticationResult> RefreshAsync(
        WorkOsRefreshRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        Dictionary<string, string> form = new(StringComparer.Ordinal)
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = options.ClientId.Trim(),
            ["client_secret"] = options.GetEffectiveClientSecret(),
            ["refresh_token"] = request.RefreshToken,
        };

        Add(form, "organization_id", request.OrganizationId);
        AddMetadata(form, request.Metadata);
        return AuthenticateAsync(form, cancellationToken);
    }

    public async Task<WorkOsMagicAuthStartResult> BeginAsync(
        WorkOsMagicAuthStartRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var message = CreateJsonRequest(
            HttpMethod.Post,
            new Uri(options.GetApiBaseUri(), options.MagicAuthPath),
            new MagicAuthCreateDto { Email = request.Email },
            includeApiKey: true);

        using var response = await httpClient.SendAsync(message, cancellationToken).ConfigureAwait(false);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var magicAuth = Deserialize<MagicAuthDto>(payload);
        return new WorkOsMagicAuthStartResult(
            magicAuth.Id ?? string.Empty,
            magicAuth.Email ?? request.Email,
            ParseDateTime(magicAuth.ExpiresAt),
            request.ReturnCode ? magicAuth.Code : null);
    }

    public async Task<WorkOsMagicAuth?> GetAsync(
        string magicAuthId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(magicAuthId);

        using var message = CreateRequest(
            HttpMethod.Get,
            new Uri(options.GetApiBaseUri(), options.MagicAuthPath.TrimEnd('/') + "/" + Uri.EscapeDataString(magicAuthId.Trim())),
            includeApiKey: true);
        using var response = await httpClient.SendAsync(message, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        var payload = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var magicAuth = Deserialize<MagicAuthDto>(payload);
        return new WorkOsMagicAuth(
            magicAuth.Id ?? magicAuthId.Trim(),
            magicAuth.Email ?? string.Empty,
            magicAuth.UserId,
            magicAuth.Code,
            ParseDateTime(magicAuth.ExpiresAt));
    }

    public async Task<WorkOsSessionSignOutResult> SignOutAsync(
        WorkOsSignOutRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var logoutUrl = BuildLogoutUrl(request.SessionId, request.ReturnToUri);
        var revoked = false;

        if (!string.IsNullOrWhiteSpace(request.SessionId))
        {
            var revokePath = options.SessionRevokePathTemplate.Replace(
                "{sessionId}",
                Uri.EscapeDataString(request.SessionId.Trim()),
                StringComparison.Ordinal);
            using var message = CreateRequest(
                HttpMethod.Post,
                new Uri(options.GetApiBaseUri(), revokePath),
                includeApiKey: true);
            using var response = await httpClient.SendAsync(message, cancellationToken).ConfigureAwait(false);
            if (response.StatusCode != HttpStatusCode.NotFound)
            {
                response.EnsureSuccessStatusCode();
                revoked = true;
            }
        }

        return new WorkOsSessionSignOutResult(revoked, logoutUrl);
    }

    private async Task<string> CreateTotpChallengeAsync(string factorId, CancellationToken cancellationToken)
    {
        using var message = CreateJsonRequest(
            HttpMethod.Post,
            new Uri(options.GetApiBaseUri(), "/auth/factors/" + Uri.EscapeDataString(factorId.Trim()) + "/challenge"),
            new { },
            includeApiKey: true);

        using var response = await httpClient.SendAsync(message, cancellationToken).ConfigureAwait(false);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var challenge = Deserialize<ChallengeDto>(payload);
        if (string.IsNullOrWhiteSpace(challenge.Id))
        {
            throw new InvalidOperationException("WorkOS did not return an authentication challenge id.");
        }

        return challenge.Id.Trim();
    }

    private async Task<WorkOsAuthenticationResult> AuthenticateAsync(
        IReadOnlyDictionary<string, string> form,
        CancellationToken cancellationToken)
    {
        using var message = CreateFormRequest(
            HttpMethod.Post,
            new Uri(options.GetAuthApiBaseUri(), options.AuthenticatePath),
            form);
        using var response = await httpClient.SendAsync(message, cancellationToken).ConfigureAwait(false);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            return new WorkOsAuthenticationSuccess(MapSession(Deserialize<AuthenticationSuccessDto>(payload)));
        }

        return MapError(payload);
    }

    private WorkOsAuthenticatedSession MapSession(AuthenticationSuccessDto dto)
    {
        var claims = WorkOsTokenClaimsParser.TryParse(dto.AccessToken, out var parsedClaims) && parsedClaims is not null
            ? new WorkOsTokenClaims(
                parsedClaims.SubjectId,
                parsedClaims.SessionId,
                string.IsNullOrWhiteSpace(parsedClaims.OrganizationId)
                    ? dto.OrganizationId
                    : parsedClaims.OrganizationId,
                parsedClaims.Roles,
                parsedClaims.Permissions,
                parsedClaims.FeatureFlags,
                parsedClaims.Entitlements,
                parsedClaims.ExpiresAtUtc)
            : new WorkOsTokenClaims(
                FirstNonEmpty(dto.User?.Id, ReadSubject(dto.AccessToken)) ?? string.Empty,
                organizationId: dto.OrganizationId);

        return new WorkOsAuthenticatedSession(
            claims.SubjectId,
            dto.AccessToken ?? string.Empty,
            claims,
            dto.RefreshToken,
            dto.User?.Email,
            BuildDisplayName(dto.User),
            dto.User?.EmailVerified,
            accessTokenExpiresAtUtc: claims.ExpiresAtUtc);
    }

    private static WorkOsAuthenticationResult MapError(string payload)
    {
        var error = Deserialize<AuthenticationErrorDto>(payload);
        var code = FirstNonEmpty(error.Code, error.Error) ?? "authentication_failed";
        var message = FirstNonEmpty(error.Message, error.ErrorDescription) ?? "Authentication failed.";

        if (string.IsNullOrWhiteSpace(error.PendingAuthenticationToken))
        {
            return new WorkOsAuthenticationFailure(new WorkOsFailure(code, message));
        }

        var pending = new WorkOsPendingAuthentication(
            error.PendingAuthenticationToken,
            error.Email ?? error.User?.Email,
            error.EmailVerificationId,
            error.AuthenticationChallengeId,
            factors: error.AuthenticationFactors?.Select(MapFactor).ToArray(),
            organizations: error.Organizations?.Select(item =>
                new WorkOsOrganizationChoice(item.Id ?? string.Empty, item.Name ?? item.Id ?? item.ExternalId ?? string.Empty)).ToArray(),
            totpEnrollment: MapTotpEnrollment(error.AuthenticationFactors));

        var kind = code switch
        {
            "email_verification_required" => WorkOsChallengeKind.EmailVerificationRequired,
            "mfa_enrollment" => WorkOsChallengeKind.MfaEnrollmentRequired,
            "mfa_challenge" => WorkOsChallengeKind.MfaChallengeRequired,
            "organization_selection_required" or "organization_selection" => WorkOsChallengeKind.OrganizationSelectionRequired,
            "identity_linking_required" => WorkOsChallengeKind.IdentityLinkingRequired,
            _ => WorkOsChallengeKind.ProviderChallengeRequired,
        };

        return new WorkOsAuthenticationChallenge(
            kind,
            pending,
            code,
            message);
    }

    private Uri? BuildLogoutUrl(string? sessionId, string? returnTo)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return null;
        }

        Dictionary<string, string> parameters = new(StringComparer.Ordinal)
        {
            ["session_id"] = sessionId.Trim(),
        };
        Add(parameters, "return_to", returnTo);

        var builder = new UriBuilder(new Uri(options.GetAuthApiBaseUri(), options.LogoutPath))
        {
            Query = ToQueryString(parameters),
        };

        return builder.Uri;
    }

    private HttpRequestMessage CreateFormRequest(HttpMethod method, Uri uri, IReadOnlyDictionary<string, string> form)
    {
        var message = CreateRequest(method, uri, includeApiKey: false);
        message.Content = new FormUrlEncodedContent(form);
        return message;
    }

    private HttpRequestMessage CreateJsonRequest(HttpMethod method, Uri uri, object payload, bool includeApiKey)
    {
        var message = CreateRequest(method, uri, includeApiKey);
        message.Content = new StringContent(
            JsonSerializer.Serialize(payload, WorkOsJsonDefaults.SerializerOptions),
            Encoding.UTF8,
            "application/json");
        return message;
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, Uri uri, bool includeApiKey)
    {
        var message = new HttpRequestMessage(method, uri);
        message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (includeApiKey)
        {
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.GetEffectiveApiKey());
        }

        return message;
    }

    private static void AddMetadata(IDictionary<string, string> values, WorkOsRequestMetadata? metadata)
    {
        Add(values, "ip_address", metadata?.IpAddress);
        Add(values, "user_agent", metadata?.UserAgent);
    }

    private static void Add(IDictionary<string, string> values, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            values[key] = value.Trim();
        }
    }

    private static T Deserialize<T>(string payload) =>
        JsonSerializer.Deserialize<T>(payload, WorkOsJsonDefaults.SerializerOptions)
        ?? throw new InvalidOperationException("Unable to deserialize WorkOS response payload.");

    private static string ToQueryString(IReadOnlyDictionary<string, string> values) =>
        string.Join("&", values.Select(static pair => Uri.EscapeDataString(pair.Key) + "=" + Uri.EscapeDataString(pair.Value)));

    private static string BuildScope(WorkOsAuthorizationRequest request)
    {
        var scopes = new[] { "openid", "profile", "email", "offline_access" }
            .Concat(request.ProviderScopes ?? Array.Empty<string>());
        return string.Join(' ', scopes.Distinct(StringComparer.Ordinal));
    }

    private static string? BuildDisplayName(UserDto? user)
    {
        if (user is null)
        {
            return null;
        }

        var parts = new[] { user.FirstName, user.LastName }
            .Where(static part => !string.IsNullOrWhiteSpace(part))
            .Select(static part => part!.Trim())
            .ToArray();

        return parts.Length == 0 ? null : string.Join(' ', parts);
    }

    private static string? ReadSubject(string? accessToken)
    {
        if (!WorkOsTokenClaimsParser.TryParse(accessToken ?? string.Empty, out var context) || context is null)
        {
            return null;
        }

        return context.SubjectId;
    }

    private static WorkOsAuthenticationFactor MapFactor(FactorDto factor) =>
        new(
            factor.Id ?? string.Empty,
            factor.Type ?? "totp");

    private static WorkOsTotpEnrollment? MapTotpEnrollment(IEnumerable<FactorDto>? factors)
    {
        var factor = factors?.FirstOrDefault(static item =>
            string.Equals(item.Type, "totp", StringComparison.OrdinalIgnoreCase)
            && item.Totp is not null);

        return factor?.Totp is null || string.IsNullOrWhiteSpace(factor.Id)
            ? null
            : new WorkOsTotpEnrollment(
                factor.Id,
                factor.Totp.Issuer,
                factor.Totp.User,
                factor.Totp.QrCode,
                factor.Totp.Secret,
                factor.Totp.Uri);
    }

    private static DateTimeOffset? ParseDateTime(string? value) =>
        DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed)
            ? parsed
            : null;

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value))?.Trim();
}
