namespace Incursa.Prose.Cli;

internal enum OutputFormat
{
    Text,
    Json,
}

internal sealed record CliOptions(
    IReadOnlyList<string> Paths,
    OutputFormat Format,
    bool ShowHelp);

internal static class CliOptionsParser
{
    public static bool TryParse(string[] args, out CliOptions options, out string error)
    {
        ArgumentNullException.ThrowIfNull(args);

        options = new CliOptions([], OutputFormat.Text, false);
        error = string.Empty;
        if (args.Length == 0 || args.Any(argument => string.Equals(argument, "--help", StringComparison.OrdinalIgnoreCase)))
        {
            options = options with { ShowHelp = true };
            return true;
        }

        if (!string.Equals(args[0], "analyze", StringComparison.OrdinalIgnoreCase))
        {
            error = "Unknown command. Expected 'analyze'.";
            return false;
        }

        var paths = new List<string>();
        var format = OutputFormat.Text;
        for (var index = 1; index < args.Length; index++)
        {
            var argument = args[index];
            if (string.Equals(argument, "--format", StringComparison.OrdinalIgnoreCase))
            {
                if (++index >= args.Length)
                {
                    error = "Missing value for --format.";
                    return false;
                }

                if (!Enum.TryParse<OutputFormat>(args[index], true, out format))
                {
                    error = "Invalid format. Use text or json.";
                    return false;
                }
            }
            else if (argument.StartsWith('-'))
            {
                error = $"Unknown option '{argument}'.";
                return false;
            }
            else
            {
                paths.Add(argument);
            }
        }

        if (paths.Count == 0)
        {
            error = "At least one file or directory path is required.";
            return false;
        }

        options = new CliOptions(paths, format, false);
        return true;
    }

    public static void WriteUsage(TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        writer.WriteLine("Usage: incursa-prose analyze [--format text|json] <path>...");
        writer.WriteLine();
        writer.WriteLine("Analyzes Markdown and plain-text files for advisory prose findings and readability metrics.");
        writer.WriteLine("Findings never change the success exit code.");
    }
}
