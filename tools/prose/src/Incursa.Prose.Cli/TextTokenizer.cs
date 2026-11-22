using System.Text.RegularExpressions;

namespace Incursa.Prose.Cli;

internal static partial class TextTokenizer
{
    public static IReadOnlyList<TextToken> Tokenize(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var lineStarts = GetLineStarts(text);
        var tokens = new List<TextToken>();

        foreach (Match match in WordExpression().Matches(text))
        {
            var lineIndex = FindLineIndex(lineStarts, match.Index);
            var normalized = match.Value.Replace('’', '\'').ToUpperInvariant();
            tokens.Add(new TextToken(
                normalized,
                match.Index,
                match.Length,
                lineIndex + 1,
                match.Index - lineStarts[lineIndex] + 1));
        }

        return tokens;
    }

    private static List<int> GetLineStarts(string text)
    {
        var starts = new List<int> { 0 };
        for (var index = 0; index < text.Length; index++)
        {
            if (text[index] == '\n')
            {
                starts.Add(index + 1);
            }
        }

        return starts;
    }

    private static int FindLineIndex(List<int> lineStarts, int position)
    {
        var index = lineStarts.BinarySearch(position);
        return index >= 0 ? index : ~index - 1;
    }

    [GeneratedRegex(@"[\p{L}\p{N}]+(?:['’\-][\p{L}\p{N}]+)*-?", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex WordExpression();
}
