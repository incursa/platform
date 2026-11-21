namespace Incursa.Prose.Cli;

internal sealed class ProseAnalyzer(Lexicon lexicon)
{
    public DocumentAnalysis Analyze(string path, string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(content);

        var extension = Path.GetExtension(path);
        var visibleText = extension.Equals(".md", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".markdown", StringComparison.OrdinalIgnoreCase)
                ? MarkdownMasker.MaskNonProse(content)
                : content;
        var tokens = TextTokenizer.Tokenize(visibleText);
        var findings = Find(content, visibleText, tokens);
        return new DocumentAnalysis(path, Readability.Measure(visibleText, tokens), findings);
    }

    private List<ProseFinding> Find(
        string source,
        string visibleText,
        IReadOnlyList<TextToken> tokens)
    {
        var findings = new List<ProseFinding>();
        for (var index = 0; index < tokens.Count; index++)
        {
            if (!lexicon.TryMatch(visibleText, tokens, index, out var match))
            {
                continue;
            }

            var first = tokens[index];
            var last = tokens[index + match.TokenCount - 1];
            var length = last.Start + last.Length - first.Start;
            findings.Add(new ProseFinding(
                first.Line,
                first.Column,
                source.Substring(first.Start, length),
                match.Term,
                match.Categories,
                Explain(match.Categories)));
            index += match.TokenCount - 1;
        }

        return findings;
    }

    private static string Explain(IReadOnlyList<ProseCategory> categories)
    {
        return string.Join(' ', categories.Select(category => category switch
        {
            ProseCategory.Filler => "May add words without adding meaning; check whether the sentence is clearer without it.",
            ProseCategory.Hedge => "Softens certainty; make sure the confidence level is intentional or name the uncertainty.",
            ProseCategory.Weasel => "Can imply support without naming evidence, scope, or an actor; consider being more specific.",
            ProseCategory.Buzzword => "May be vague outside a specialist audience; consider a concrete description.",
            _ => throw new InvalidOperationException($"Unknown prose category '{category}'."),
        }));
    }
}
