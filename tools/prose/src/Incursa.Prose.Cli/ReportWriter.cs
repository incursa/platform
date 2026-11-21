using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Incursa.Prose.Cli;

internal static class ReportWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    public static void Write(AnalysisReport report, OutputFormat format, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(writer);

        if (format == OutputFormat.Json)
        {
            writer.WriteLine(JsonSerializer.Serialize(report, JsonOptions));
            return;
        }

        foreach (var document in report.Documents)
        {
            writer.WriteLine(document.Path);
            writer.WriteLine(string.Create(
                CultureInfo.InvariantCulture,
                $"  {document.Metrics.Words} words, {document.Metrics.Sentences} sentences, {document.Metrics.Syllables} syllables; Flesch {FormatScore(document.Metrics.FleschReadingEase)}, grade {FormatScore(document.Metrics.FleschKincaidGradeLevel)}"));

            foreach (var finding in document.Findings)
            {
                var categories = string.Join(", ", finding.Categories.Select(GetCategoryName));
                writer.WriteLine(string.Create(
                    CultureInfo.InvariantCulture,
                    $"  {finding.Line}:{finding.Column} [{categories}] \"{finding.Text}\" — {finding.Explanation}"));
            }

            if (document.Findings.Count == 0)
            {
                writer.WriteLine("  No lexical findings.");
            }
        }

        writer.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"Analyzed {report.Summary.Documents} document(s); {report.Summary.Findings} advisory finding(s)."));
    }

    private static string FormatScore(double? score) => score?.ToString("0.00", CultureInfo.InvariantCulture) ?? "n/a";

    private static string GetCategoryName(ProseCategory category) => category switch
    {
        ProseCategory.Filler => "filler",
        ProseCategory.Hedge => "hedge",
        ProseCategory.Weasel => "weasel",
        ProseCategory.Buzzword => "buzzword",
        _ => throw new InvalidOperationException($"Unknown prose category '{category}'."),
    };
}
