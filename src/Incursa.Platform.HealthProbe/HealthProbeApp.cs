using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Incursa.Platform.Health;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Incursa.Platform.HealthProbe;

/// <summary>
/// Command-line entry points for running health probes.
/// </summary>
public static class HealthProbeApp
{
    /// <summary>
    /// Determines whether the arguments represent a health check invocation.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns>True when the first argument is the health check command.</returns>
    public static bool IsHealthCheckInvocation(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        return args.Length > 0 && args[0].Equals(HealthProbeDefaults.CommandName, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Executes a health check if invoked and returns an exit code.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <param name="services">Service provider for resolving dependencies.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The exit code, or -1 when not invoked.</returns>
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "CLI entry point maps unexpected exceptions to exit codes.")]
    public static async Task<int> TryRunHealthCheckAndExitAsync(
        string[] args,
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(services);

        if (!IsHealthCheckInvocation(args))
        {
            return -1;
        }

        try
        {
            return await RunHealthCheckAsync(args, services, cancellationToken).ConfigureAwait(false);
        }
        catch (HealthProbeArgumentException ex)
        {
            await Console.Error.WriteLineAsync(ex.ToString()).ConfigureAwait(false);
            return HealthProbeExitCodes.Misconfiguration;
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync(ex.ToString()).ConfigureAwait(false);
            return HealthProbeExitCodes.Misconfiguration;
        }
    }

    /// <summary>
    /// Runs a health check and returns the exit code.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <param name="services">Service provider for resolving dependencies.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The exit code for the health check.</returns>
    public static async Task<int> RunHealthCheckAsync(
        string[] args,
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(services);

        var commandLine = HealthProbeCommandLine.Parse(args);
        var baseOptions = services.GetService<IOptions<HealthProbeOptions>>()?.Value ?? new HealthProbeOptions();
        var options = baseOptions.Clone();

        if (commandLine.ListBuckets)
        {
            WriteListOutput(commandLine);
            return HealthProbeExitCodes.Healthy;
        }

        if (commandLine.TimeoutOverride.HasValue)
        {
            options.Timeout = commandLine.TimeoutOverride.Value;
        }

        var bucketName = commandLine.BucketName ?? options.DefaultBucket;
        if (string.IsNullOrWhiteSpace(bucketName))
        {
            throw new HealthProbeArgumentException("A health bucket is required.");
        }

        var runner = services.GetService<IHealthProbeRunner>();
        if (runner is null)
        {
            throw new InvalidOperationException("IHealthProbeRunner is not registered. Call AddIncursaHealthProbe().");
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(options.Timeout);

        HealthProbeResult result;
        try
        {
            result = await runner.RunAsync(
                new HealthProbeRequest(bucketName, options.IncludeData || commandLine.IncludeData),
                timeoutCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            Console.WriteLine($"Non-healthy [{bucketName}] timed out after {options.Timeout.TotalSeconds.ToString(CultureInfo.InvariantCulture)} seconds");
            return HealthProbeExitCodes.NonHealthy;
        }
        catch (HealthProbeArgumentException)
        {
            throw;
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync(ex.ToString()).ConfigureAwait(false);
            return HealthProbeExitCodes.Misconfiguration;
        }

        WriteOutput(commandLine, result);
        return result.ExitCode;
    }

    private static void WriteOutput(HealthProbeCommandLine commandLine, HealthProbeResult result)
    {
        if (commandLine.JsonOutput)
        {
            Console.WriteLine(JsonSerializer.Serialize(result.Payload));
            return;
        }

        Console.WriteLine($"{result.Status} [{result.Bucket}] in {(int)Math.Round(result.Duration.TotalMilliseconds)} ms");
    }

    private static void WriteListOutput(HealthProbeCommandLine commandLine)
    {
        if (commandLine.JsonOutput)
        {
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                buckets = new[] { PlatformHealthTags.Live, PlatformHealthTags.Ready, PlatformHealthTags.Dep },
            }));
            return;
        }

        Console.WriteLine(PlatformHealthTags.Live);
        Console.WriteLine(PlatformHealthTags.Ready);
        Console.WriteLine(PlatformHealthTags.Dep);
    }
}
