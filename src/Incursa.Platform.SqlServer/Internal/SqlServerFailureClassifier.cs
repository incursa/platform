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

using Microsoft.Data.SqlClient;

namespace Incursa.Platform;

internal static class SqlServerFailureClassifier
{
    private enum SqlServerFailureCategory
    {
        Unknown = 0,
        Cancellation,
        Timeout,
        Transport,
        PreLoginHandshake,
        Firewall,
    }

    public static bool IsInfrastructureFailure(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return TryGetCategory(exception, out _);
    }

    public static bool ShouldRetry(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (!TryGetCategory(exception, out var category))
        {
            return false;
        }

        return category is SqlServerFailureCategory.Timeout
            or SqlServerFailureCategory.Transport
            or SqlServerFailureCategory.PreLoginHandshake;
    }

    public static string GetCategoryName(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return TryGetCategory(exception, out var category) ? category.ToString() : SqlServerFailureCategory.Unknown.ToString();
    }

    private static bool TryGetCategory(Exception exception, out SqlServerFailureCategory category)
    {
        foreach (var current in EnumerateExceptions(exception))
        {
            if (current is OperationCanceledException)
            {
                category = SqlServerFailureCategory.Cancellation;
                return true;
            }

            if (current is TimeoutException)
            {
                category = SqlServerFailureCategory.Timeout;
                return true;
            }

            if (current is SqlException sqlException)
            {
                var message = sqlException.Message ?? string.Empty;

                if (sqlException.Number == -2 || ContainsIgnoreCase(message, "Execution Timeout Expired"))
                {
                    category = SqlServerFailureCategory.Timeout;
                    return true;
                }

                if (ContainsIgnoreCase(message, "Operation cancelled by user"))
                {
                    category = SqlServerFailureCategory.Cancellation;
                    return true;
                }

                if (ContainsIgnoreCase(message, "Client IP") && ContainsIgnoreCase(message, "is not allowed"))
                {
                    category = SqlServerFailureCategory.Firewall;
                    return true;
                }

                if (ContainsIgnoreCase(message, "pre-login handshake")
                    || ContainsIgnoreCase(message, "SSL Provider, error: 31"))
                {
                    category = SqlServerFailureCategory.PreLoginHandshake;
                    return true;
                }

                if (ContainsIgnoreCase(message, "transport-level error")
                    || ContainsIgnoreCase(message, "TCP Provider")
                    || sqlException.Number is 53 or 64 or 233 or 10053 or 10054 or 10060)
                {
                    category = SqlServerFailureCategory.Transport;
                    return true;
                }
            }
        }

        category = SqlServerFailureCategory.Unknown;
        return false;
    }

    private static IEnumerable<Exception> EnumerateExceptions(Exception exception)
    {
        yield return exception;

        if (exception is AggregateException aggregateException)
        {
            foreach (var inner in aggregateException.InnerExceptions)
            {
                foreach (var nested in EnumerateExceptions(inner))
                {
                    yield return nested;
                }
            }

            yield break;
        }

        if (exception.InnerException == null)
        {
            yield break;
        }

        foreach (var nested in EnumerateExceptions(exception.InnerException))
        {
            yield return nested;
        }
    }

    private static bool ContainsIgnoreCase(string text, string value)
    {
        return text.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
