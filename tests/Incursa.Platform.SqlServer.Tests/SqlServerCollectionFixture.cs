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


using System.Linq;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Data.SqlClient;

namespace Incursa.Platform.Tests;
/// <summary>
/// Shared SQL Server container fixture for all database integration tests.
/// This reduces test execution time by reusing a single Docker container across multiple test classes.
/// Each test class gets its own database within the shared container to ensure isolation.
/// </summary>
public sealed class SqlServerCollectionFixture : IAsyncLifetime
{
    private const string SaPassword = "Str0ng!Passw0rd!";
    private readonly SemaphoreSlim recoveryLock = new(1, 1);
    private IContainer msSqlContainer;
    private string? connectionString;
    private int databaseCounter;
    private bool isAvailable;

    public SqlServerCollectionFixture()
    {
        isAvailable = true;
        msSqlContainer = BuildContainer();
    }

    /// <summary>
    /// Gets the master connection string for the SQL Server container.
    /// </summary>
    public string MasterConnectionString
    {
        get
        {
            EnsureAvailable();
            return connectionString ?? throw new InvalidOperationException("Container has not been started yet.");
        }
    }

    public async ValueTask InitializeAsync()
    {
        if (!isAvailable)
        {
            return;
        }

        try
        {
            await EnsureContainerReadyAsync(forceRecreate: false, TestContext.Current.CancellationToken).ConfigureAwait(false);
        }
        catch (NotSupportedException ex) when (ex.ToString().Contains("sqlcmd", StringComparison.OrdinalIgnoreCase))
        {
            isAvailable = false;
            return;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await msSqlContainer.DisposeAsync().ConfigureAwait(false);
        recoveryLock.Dispose();
    }

    /// <summary>
    /// Creates a new isolated database for a test class within the shared container.
    /// This ensures test isolation while sharing the same Docker container.
    /// </summary>
    /// <returns>A connection string to the newly created database.</returns>
    public async Task<string> CreateTestDatabaseAsync(string name)
    {
        EnsureAvailable();
        var didRecover = false;
        while (true)
        {
            try
            {
                await EnsureContainerReadyAsync(forceRecreate: false, TestContext.Current.CancellationToken).ConfigureAwait(false);
                var effectiveConnectionString = connectionString ?? throw new InvalidOperationException("Container has not been initialized. Ensure InitializeAsync has been called before creating databases.");
                var dbNumber = Interlocked.Increment(ref databaseCounter);
                var dbName = $"TestDb_{dbNumber}_{name}_{Guid.NewGuid():N}";

                var connection = new SqlConnection(effectiveConnectionString);
                await using (connection.ConfigureAwait(false))
                {
                    await connection.OpenAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
                    var command = new SqlCommand($"CREATE DATABASE [{dbName}]", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.CommandTimeout = 120;
                        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);

                        // Build connection string for the new database
                        var builder = new SqlConnectionStringBuilder(effectiveConnectionString)
                        {
                            InitialCatalog = dbName
                        };

                        return builder.ConnectionString;
                    }
                }
            }
            catch (SqlException ex) when (IsRecoverableSqlServerFailure(ex) && !didRecover)
            {
                didRecover = true;
                await EnsureContainerReadyAsync(forceRecreate: true, TestContext.Current.CancellationToken).ConfigureAwait(false);
            }
            catch (InvalidOperationException ex) when (IsRecoverableOperationFailure(ex) && !didRecover)
            {
                didRecover = true;
                await EnsureContainerReadyAsync(forceRecreate: true, TestContext.Current.CancellationToken).ConfigureAwait(false);
            }
            catch
            {
                throw;
            }
        }
    }

    internal void EnsureAvailable()
    {
        if (!isAvailable)
        {
            throw new InvalidOperationException("SQL Server tests cannot run because the SQL Server container could not be started.");
        }
    }

    private static async Task WaitForServerReadyAsync(string connectionString, CancellationToken cancellationToken)
    {
        var timeoutAt = DateTimeOffset.UtcNow.AddSeconds(60);

        while (DateTimeOffset.UtcNow < timeoutAt)
        {
            try
            {
                var connection = new SqlConnection(connectionString);
                await using (connection.ConfigureAwait(false))
                {
                    await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                    return;
                }
            }
            catch (SqlException)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
            }
            catch (InvalidOperationException)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
            }
        }

        throw new TimeoutException("SQL Server did not become available before the timeout.");
    }

    private static bool IsRecoverableSqlServerFailure(SqlException ex)
    {
        var message = ex.ToString();
        return message.Contains("tempdb", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("transport-level error", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("A severe error occurred on the current command", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("Execution Timeout Expired", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("wait operation timed out", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsRecoverableOperationFailure(InvalidOperationException ex)
    {
        return ex.ToString().Contains("The connection is broken", StringComparison.OrdinalIgnoreCase);
    }

    private async Task EnsureContainerReadyAsync(bool forceRecreate, CancellationToken cancellationToken)
    {
        await recoveryLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (forceRecreate)
            {
                await RecreateContainerAsync(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await msSqlContainer.StartAsync(cancellationToken).ConfigureAwait(false);
            }

            connectionString = BuildMasterConnectionString();
            await WaitForServerReadyAsync(connectionString, cancellationToken).ConfigureAwait(false);
            await EnsureTempDbOnlineAsync(connectionString, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            recoveryLock.Release();
        }
    }

    private async Task RecreateContainerAsync(CancellationToken cancellationToken)
    {
        await msSqlContainer.DisposeAsync().ConfigureAwait(false);
        msSqlContainer = BuildContainer();
        await msSqlContainer.StartAsync(cancellationToken).ConfigureAwait(false);
    }

    private IContainer BuildContainer()
    {
        return new ContainerBuilder("mcr.microsoft.com/mssql/server:2022-CU10-ubuntu-22.04")
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithEnvironment("MSSQL_SA_PASSWORD", SaPassword)
            .WithEnvironment("MSSQL_PID", "Developer")
            .WithPortBinding(1433, true)
            .WithReuse(false)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(1433))
            .Build();
    }

    private string BuildMasterConnectionString()
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = $"{msSqlContainer.Hostname},{msSqlContainer.GetMappedPublicPort(1433)}",
            UserID = "sa",
            Password = SaPassword,
            InitialCatalog = "master",
            Encrypt = false,
            TrustServerCertificate = true,
        };

        return builder.ConnectionString;
    }

    private static async Task EnsureTempDbOnlineAsync(string connectionString, CancellationToken cancellationToken)
    {
        var timeoutAt = DateTimeOffset.UtcNow.AddSeconds(60);
        while (DateTimeOffset.UtcNow < timeoutAt)
        {
            try
            {
                var connection = new SqlConnection(connectionString);
                await using (connection.ConfigureAwait(false))
                {
                    await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

                var command = new SqlCommand(
                    "SELECT state_desc FROM sys.databases WHERE name = 'tempdb'",
                    connection);
                await using (command.ConfigureAwait(false))
                {
                    var state = (string?)await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                if (string.Equals(state, "ONLINE", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
                }
                }
            }
            catch (SqlException)
            {
            }

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
        }

        throw new TimeoutException("SQL Server tempdb did not report ONLINE before timeout.");
    }
}
