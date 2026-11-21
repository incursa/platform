using System.Text.Json;

namespace Incursa.Prose.Cli.Tests;

public sealed class ProgramTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task RunAsync_ReturnsSuccessAndJsonWhenFindingsExist()
    {
        var directory = Directory.CreateTempSubdirectory("incursa-prose-tests-");
        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(directory.FullName, "sample.md"),
                "This is basically a paradigm shift.",
                TestContext.Current.CancellationToken);
            using var output = new StringWriter();
            using var error = new StringWriter();

            var exitCode = await Program.RunAsync(
                ["analyze", "--format", "json", "sample.md"],
                output,
                error,
                directory.FullName);

            exitCode.ShouldBe(0);
            error.ToString().ShouldBeEmpty();
            using var document = JsonDocument.Parse(output.ToString());
            document.RootElement.GetProperty("version").GetInt32().ShouldBe(1);
            document.RootElement.GetProperty("summary").GetProperty("findings").GetInt32().ShouldBeGreaterThan(0);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task RunAsync_ReturnsFailureForMissingPath()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await Program.RunAsync(
            ["analyze", "missing.md"],
            output,
            error,
            Directory.GetCurrentDirectory());

        exitCode.ShouldBe(1);
        error.ToString().ShouldContain("does not exist");
    }
}
