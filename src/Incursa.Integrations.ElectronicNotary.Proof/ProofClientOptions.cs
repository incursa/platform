namespace Incursa.Integrations.ElectronicNotary.Proof;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Configures the Proof API client.
/// </summary>
public sealed class ProofClientOptions
{
    /// <summary>
    /// The Fairfax sandbox base URL.
    /// </summary>
    [SuppressMessage("Minor Code Smell", "S1075:URIs should not be hardcoded", Justification = "Proof exposes fixed environment endpoints.")]
    public static readonly Uri FairfaxBaseUrl = new Uri("https://api.fairfax.proof.com/");

    /// <summary>
    /// The production base URL.
    /// </summary>
    [SuppressMessage("Minor Code Smell", "S1075:URIs should not be hardcoded", Justification = "Proof exposes fixed environment endpoints.")]
    public static readonly Uri ProductionBaseUrl = new Uri("https://api.proof.com/");

    /// <summary>
    /// Gets or sets the target Proof environment when <see cref="BaseUrl"/> is not provided.
    /// </summary>
    public ProofEnvironment Environment { get; set; } = ProofEnvironment.Production;

    /// <summary>
    /// Gets or sets an explicit API base URL. When set, this overrides <see cref="Environment"/>.
    /// </summary>
    public Uri? BaseUrl { get; set; }

    /// <summary>
    /// Gets or sets the API key sent using the <c>ApiKey</c> header.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the HTTP timeout for Proof API requests.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(100);

    /// <summary>
    /// Gets or sets a value indicating whether outbound Proof API resilience is enabled.
    /// </summary>
    public bool EnableResilience { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of retry attempts for transient failures.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the initial retry backoff delay.
    /// </summary>
    public TimeSpan InitialBackoff { get; set; } = TimeSpan.FromMilliseconds(250);

    /// <summary>
    /// Gets or sets the maximum retry backoff delay.
    /// </summary>
    public TimeSpan MaxBackoff { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets the set of HTTP status codes considered transient for retries.
    /// </summary>
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Options binding requires a settable collection.")]
    public IList<int> RetryOnStatusCodes { get; set; } = new List<int> { 408, 429, 500, 502, 503, 504 };

    /// <summary>
    /// Gets or sets a value indicating whether non-idempotent HTTP methods may be retried.
    /// </summary>
    public bool RetryUnsafeMethods { get; set; }

    /// <summary>
    /// Gets or sets the number of consecutive transient failures required to open the circuit.
    /// </summary>
    public int CircuitBreakerFailureThreshold { get; set; } = 5;

    /// <summary>
    /// Gets or sets the duration the circuit remains open after tripping.
    /// </summary>
    public TimeSpan CircuitBreakDuration { get; set; } = TimeSpan.FromSeconds(30);
}
