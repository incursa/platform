using System.Reflection;

namespace Incursa.Prose.Cli;

internal sealed class Lexicon
{
    private static readonly Lazy<Lexicon> DefaultLexicon = new(CreateDefault);
    private readonly IReadOnlyDictionary<string, IReadOnlyList<LexiconEntry>> entriesByFirstToken;

    private Lexicon(IReadOnlyDictionary<string, IReadOnlyList<LexiconEntry>> entriesByFirstToken)
    {
        this.entriesByFirstToken = entriesByFirstToken;
    }

    public static Lexicon Default => DefaultLexicon.Value;

    public bool TryMatch(string source, IReadOnlyList<TextToken> tokens, int startIndex, out LexiconMatch match)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(tokens);

        if (!entriesByFirstToken.TryGetValue(tokens[startIndex].Value, out var candidates))
        {
            match = default;
            return false;
        }

        foreach (var candidate in candidates)
        {
            if (candidate.Tokens.Count > tokens.Count - startIndex)
            {
                continue;
            }

            var matches = true;
            for (var offset = 0; offset < candidate.Tokens.Count; offset++)
            {
                if (!string.Equals(candidate.Tokens[offset], tokens[startIndex + offset].Value, StringComparison.Ordinal))
                {
                    matches = false;
                    break;
                }

                if (offset > 0 && !HasWhitespaceOnlyGap(source, tokens[startIndex + offset - 1], tokens[startIndex + offset]))
                {
                    matches = false;
                    break;
                }
            }

            if (matches)
            {
                match = new LexiconMatch(candidate.Term, candidate.Categories, candidate.Tokens.Count);
                return true;
            }
        }

        match = default;
        return false;
    }

    private static bool HasWhitespaceOnlyGap(string source, TextToken previous, TextToken current)
    {
        var gapStart = previous.Start + previous.Length;
        for (var index = gapStart; index < current.Start; index++)
        {
            if (!char.IsWhiteSpace(source[index]))
            {
                return false;
            }
        }

        return true;
    }

    internal static Lexicon Create(IEnumerable<(string Term, ProseCategory Category)> terms)
    {
        ArgumentNullException.ThrowIfNull(terms);

        var merged = new Dictionary<string, MutableLexiconEntry>(StringComparer.Ordinal);
        foreach (var (term, category) in terms)
        {
            var termTokens = TextTokenizer.Tokenize(term).Select(token => token.Value).ToArray();
            if (termTokens.Length == 0)
            {
                continue;
            }

            var key = string.Join('\u001F', termTokens);
            if (!merged.TryGetValue(key, out var entry))
            {
                entry = new MutableLexiconEntry(term, termTokens);
                merged.Add(key, entry);
            }

            entry.Categories.Add(category);
        }

        var byFirstToken = merged.Values
            .Select(entry => new LexiconEntry(
                entry.Term,
                entry.Tokens,
                entry.Categories.OrderBy(category => category).ToArray()))
            .GroupBy(entry => entry.Tokens[0], StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<LexiconEntry>)group
                    .OrderByDescending(entry => entry.Tokens.Count)
                    .ThenBy(entry => entry.Term, StringComparer.Ordinal)
                    .ToArray(),
                StringComparer.Ordinal);

        return new Lexicon(byFirstToken);
    }

    private static Lexicon CreateDefault()
    {
        var terms = new List<(string Term, ProseCategory Category)>();
        LoadResource("fillers.txt", ProseCategory.Filler, terms);
        LoadResource("hedges.txt", ProseCategory.Hedge, terms);
        LoadResource("weasels.txt", ProseCategory.Weasel, terms);
        LoadResource("buzzwords.txt", ProseCategory.Buzzword, terms);
        return Create(terms);
    }

    private static void LoadResource(
        string fileName,
        ProseCategory category,
        List<(string Term, ProseCategory Category)> terms)
    {
        var resourceName = $"Incursa.Prose.Cli.Data.{fileName}";
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded lexicon resource '{resourceName}' was not found.");
        using var reader = new StreamReader(stream);

        while (reader.ReadLine() is { } line)
        {
            var term = line.Trim();
            if (term.Length > 0)
            {
                terms.Add((term, category));
            }
        }
    }

    private sealed class MutableLexiconEntry(string term, IReadOnlyList<string> tokens)
    {
        public string Term { get; } = term;

        public IReadOnlyList<string> Tokens { get; } = tokens;

        public HashSet<ProseCategory> Categories { get; } = [];
    }

    private sealed record LexiconEntry(
        string Term,
        IReadOnlyList<string> Tokens,
        IReadOnlyList<ProseCategory> Categories);
}

internal readonly record struct LexiconMatch(
    string Term,
    IReadOnlyList<ProseCategory> Categories,
    int TokenCount);
