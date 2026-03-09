namespace Incursa.Integrations.ElectronicNotary.Proof;

using System.Net;

/// <summary>
/// Represents an unsuccessful response from the Proof HTTP API.
/// </summary>
public sealed class ProofApiException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProofApiException"/> class.
    /// </summary>
    public ProofApiException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProofApiException"/> class with an error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public ProofApiException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProofApiException"/> class with an error message and inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The underlying exception.</param>
    public ProofApiException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProofApiException"/> class with HTTP response details.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="statusCode">The HTTP status code returned by Proof.</param>
    /// <param name="responseBody">The raw response body returned by Proof.</param>
    /// <param name="correlationInfo">Optional correlation header details.</param>
    /// <param name="innerException">The underlying exception.</param>
    public ProofApiException(string message, HttpStatusCode statusCode, string responseBody, string? correlationInfo = null, Exception? innerException = null)
        : base(message, innerException)
    {
        this.StatusCode = statusCode;
        this.ResponseBody = responseBody;
        this.CorrelationInfo = correlationInfo;
    }

    /// <summary>
    /// Gets the HTTP status code returned by the Proof API.
    /// </summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>
    /// Gets the raw response body returned by the Proof API.
    /// </summary>
    public string ResponseBody { get; } = string.Empty;

    /// <summary>
    /// Gets optional correlation header information from the response.
    /// </summary>
    public string? CorrelationInfo { get; }
}
