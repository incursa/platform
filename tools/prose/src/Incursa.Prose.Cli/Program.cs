namespace Incursa.Prose.Cli;

internal static class Program
{
    private static Task<int> Main(string[] args) => RunAsync(args, Console.Out, Console.Error, Directory.GetCurrentDirectory());

    internal static async Task<int> RunAsync(
        string[] args,
        TextWriter output,
        TextWriter error,
        string currentDirectory)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentDirectory);

        if (!CliOptionsParser.TryParse(args, out var options, out var parseError))
        {
            await error.WriteLineAsync(parseError).ConfigureAwait(false);
            CliOptionsParser.WriteUsage(error);
            return 1;
        }

        if (options.ShowHelp)
        {
            CliOptionsParser.WriteUsage(output);
            return 0;
        }

        try
        {
            var analyzer = new ProseAnalyzer(Lexicon.Default);
            var documents = new List<DocumentAnalysis>();
            foreach (var fullPath in FileDiscovery.Resolve(options.Paths, currentDirectory))
            {
                var content = await File.ReadAllTextAsync(fullPath).ConfigureAwait(false);
                documents.Add(analyzer.Analyze(FileDiscovery.GetDisplayPath(fullPath, currentDirectory), content));
            }

            var report = new AnalysisReport(
                1,
                documents,
                new AnalysisSummary(documents.Count, documents.Sum(document => document.Findings.Count)));
            ReportWriter.Write(report, options.Format, output);
            return 0;
        }
        catch (Exception exception) when (exception is
            IOException or
            UnauthorizedAccessException or
            NotSupportedException or
            ArgumentException or
            System.Security.SecurityException or
            System.Text.RegularExpressions.RegexMatchTimeoutException or
            System.Text.Json.JsonException)
        {
            await error.WriteLineAsync(exception.Message).ConfigureAwait(false);
            return 1;
        }
    }
}
