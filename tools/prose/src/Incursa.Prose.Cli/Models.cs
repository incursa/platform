namespace Incursa.Prose.Cli;

internal enum ProseCategory
{
    Filler,
    Hedge,
    Weasel,
    Buzzword,
}

internal sealed record ProseFinding(
    int Line,
    int Column,
    string Text,
    string Term,
    IReadOnlyList<ProseCategory> Categories,
    string Explanation);

internal sealed record ProseMetrics(
    int Sentences,
    int Words,
    int Syllables,
    double? FleschReadingEase,
    double? FleschKincaidGradeLevel);

internal sealed record DocumentAnalysis(
    string Path,
    ProseMetrics Metrics,
    IReadOnlyList<ProseFinding> Findings);

internal sealed record AnalysisReport(
    int Version,
    IReadOnlyList<DocumentAnalysis> Documents,
    AnalysisSummary Summary);

internal sealed record AnalysisSummary(int Documents, int Findings);

internal readonly record struct TextToken(
    string Value,
    int Start,
    int Length,
    int Line,
    int Column);
