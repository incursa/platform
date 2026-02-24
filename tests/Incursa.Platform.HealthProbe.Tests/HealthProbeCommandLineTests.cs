using Microsoft.Extensions.DependencyInjection;

namespace Incursa.Platform.HealthProbe.Tests;

/// <summary>
/// Tests for health probe command-line parsing and execution.
/// </summary>
public sealed class HealthProbeCommandLineTests
{
    private static readonly string[] DefaultArgs = { "health" };
    private static readonly string[] DepArgs = { "health", "dep" };
    private static readonly string[] UnknownFlagArgs = { "health", "--nope" };
    private static readonly string[] OverridesArgs =
    {
        "health",
        "ready",
        "--timeout",
        "5",
        "--include-data",
        "--json",
    };
    /// <summary>Given only the base command, then parsing leaves BucketName null and JsonOutput false.</summary>
    /// <intent>Describe default parsing with no bucket or flags.</intent>
    /// <scenario>Given arguments containing only "health".</scenario>
    /// <behavior>BucketName remains null and JsonOutput stays false.</behavior>
    [Fact]
    public void ParseDefaultsToConfiguredEndpoint()
    {
        var commandLine = HealthProbeCommandLine.Parse(DefaultArgs);

        commandLine.BucketName.ShouldBeNull();
        commandLine.JsonOutput.ShouldBeFalse();
    }

    /// <summary>Given an explicit bucket argument, then parsing uses it as BucketName.</summary>
    /// <intent>Describe parsing of a positional bucket argument.</intent>
    /// <scenario>Given arguments "health" and "dep".</scenario>
    /// <behavior>BucketName is "dep".</behavior>
    [Fact]
    public void ParseUsesExplicitBucketName()
    {
        var commandLine = HealthProbeCommandLine.Parse(DepArgs);

        commandLine.BucketName.ShouldBe("dep");
    }

    /// <summary>When override flags are provided, then parsing populates each override option.</summary>
    /// <intent>Describe parsing of timeout, include-data, and json flags.</intent>
    /// <scenario>Given arguments with timeout, include-data, and json options.</scenario>
    /// <behavior>BucketName and all override properties match the provided values.</behavior>
    [Fact]
    public void ParseUsesOverrides()
    {
        var commandLine = HealthProbeCommandLine.Parse(OverridesArgs);

        commandLine.BucketName.ShouldBe("ready");
        commandLine.TimeoutOverride.ShouldBe(TimeSpan.FromSeconds(5));
        commandLine.IncludeData.ShouldBeTrue();
        commandLine.JsonOutput.ShouldBeTrue();
    }

    /// <summary>Given health checks are not registered, then running the health command returns Misconfiguration.</summary>
    /// <intent>Describe validation when host DI is missing health check services.</intent>
    /// <scenario>Given a service provider and a command line containing only "health".</scenario>
    /// <behavior>The returned exit code is Misconfiguration.</behavior>
    [Fact]
    public async Task TryRunReturnsMisconfigurationWhenHealthServiceMissing()
    {
        var services = new ServiceCollection()
            .AddIncursaHealthProbe()
            .BuildServiceProvider();

        var exitCode = await HealthProbeApp.TryRunHealthCheckAndExitAsync(
            DefaultArgs,
            services,
            CancellationToken.None);

        exitCode.ShouldBe(HealthProbeExitCodes.Misconfiguration);
    }

    /// <summary>When an unknown flag is provided, then parsing throws a HealthProbeArgumentException.</summary>
    /// <intent>Describe parsing failure for unsupported command line options.</intent>
    /// <scenario>Given arguments including an unrecognized "--nope" option.</scenario>
    /// <behavior>Parsing throws and the message mentions an unknown option.</behavior>
    [Fact]
    public void ParseThrowsForUnknownFlag()
    {
        var exception = Should.Throw<HealthProbeArgumentException>(() =>
            HealthProbeCommandLine.Parse(UnknownFlagArgs));

        exception.Message.ShouldContain("Unknown option");
    }
}
