// Copyright (c) Incursa
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Incursa.Platform;

/// <summary>
/// Provides bounded error text suitable for persistence in outbox retry/failure metadata.
/// </summary>
internal static class OutboxFailureText
{
    internal const int MaxLength = 2048;
    private const string FallbackMessage = "Failure details unavailable.";
    private const string TruncatedSuffix = "... [truncated]";

    /// <summary>
    /// Builds a bounded failure text string from an exception.
    /// </summary>
    /// <param name="exception">Source exception.</param>
    /// <returns>Normalized bounded text.</returns>
    public static string FromException(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var message = $"{exception.GetType().Name}: {exception.Message}";

        if (exception.InnerException != null)
        {
            message = string.Concat(
                message,
                " | Inner: ",
                exception.InnerException.GetType().Name,
                ": ",
                exception.InnerException.Message);
        }

        return Normalize(message);
    }

    /// <summary>
    /// Normalizes and bounds a free-form failure message.
    /// </summary>
    /// <param name="message">Failure message to normalize.</param>
    /// <returns>Normalized bounded text.</returns>
    public static string Normalize(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return FallbackMessage;
        }

        var trimmed = message.Trim();
        if (trimmed.Length <= MaxLength)
        {
            return trimmed;
        }

        var maxPrefixLength = MaxLength - TruncatedSuffix.Length;
        if (maxPrefixLength <= 0)
        {
            return TruncatedSuffix;
        }

        return string.Concat(trimmed.AsSpan(0, maxPrefixLength), TruncatedSuffix);
    }
}
