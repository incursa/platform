using System.Text.RegularExpressions;

namespace Incursa.Prose.Cli;

internal static partial class MarkdownMasker
{
    public static string MaskNonProse(string markdown)
    {
        ArgumentNullException.ThrowIfNull(markdown);

        var buffer = markdown.ToCharArray();
        MaskBlockRegions(markdown, buffer);
        MaskMatches(markdown, buffer, InlineCodeExpression());
        MaskLinks(markdown, buffer, InlineLinkExpression());
        MaskLinks(markdown, buffer, ReferenceLinkExpression());
        MaskMatches(markdown, buffer, AutolinkExpression());
        MaskMatches(markdown, buffer, RawUrlExpression());
        MaskMatches(markdown, buffer, HtmlCommentExpression());
        MaskMatches(markdown, buffer, HtmlTagExpression());
        return new string(buffer);
    }

    private static void MaskBlockRegions(string markdown, char[] buffer)
    {
        var lineStart = 0;
        var lineNumber = 0;
        var inFrontMatter = false;
        char? fenceCharacter = null;
        var fenceLength = 0;

        while (lineStart <= markdown.Length)
        {
            var newline = markdown.IndexOf('\n', lineStart);
            var lineEnd = newline >= 0 ? newline : markdown.Length;
            var contentEnd = lineEnd > lineStart && markdown[lineEnd - 1] == '\r' ? lineEnd - 1 : lineEnd;
            var line = markdown[lineStart..contentEnd];
            var trimmed = line.Trim();
            var trimmedStart = line.TrimStart();
            var maskLine = false;

            if (lineNumber == 0 && string.Equals(trimmed, "---", StringComparison.Ordinal))
            {
                inFrontMatter = true;
                maskLine = true;
            }
            else if (inFrontMatter)
            {
                maskLine = true;
                if (string.Equals(trimmed, "---", StringComparison.Ordinal)
                    || string.Equals(trimmed, "...", StringComparison.Ordinal))
                {
                    inFrontMatter = false;
                }
            }
            else if (fenceCharacter is not null)
            {
                maskLine = true;
                if (IsFence(trimmedStart, fenceCharacter.Value, fenceLength))
                {
                    fenceCharacter = null;
                    fenceLength = 0;
                }
            }
            else if (TryGetFence(trimmedStart, out var candidateCharacter, out var candidateLength))
            {
                fenceCharacter = candidateCharacter;
                fenceLength = candidateLength;
                maskLine = true;
            }
            else if (line.StartsWith("    ", StringComparison.Ordinal)
                || line.StartsWith('\t'))
            {
                maskLine = true;
            }
            else if (ReferenceDefinitionExpression().IsMatch(line))
            {
                maskLine = true;
            }

            if (maskLine)
            {
                MaskRange(buffer, lineStart, contentEnd - lineStart);
            }

            if (newline < 0)
            {
                break;
            }

            lineStart = newline + 1;
            lineNumber++;
        }
    }

    private static bool TryGetFence(string line, out char fenceCharacter, out int fenceLength)
    {
        fenceCharacter = default;
        fenceLength = 0;
        if (line.Length < 3 || (line[0] != '`' && line[0] != '~'))
        {
            return false;
        }

        fenceCharacter = line[0];
        while (fenceLength < line.Length && line[fenceLength] == fenceCharacter)
        {
            fenceLength++;
        }

        return fenceLength >= 3;
    }

    private static bool IsFence(string line, char fenceCharacter, int minimumLength)
    {
        var length = 0;
        while (length < line.Length && line[length] == fenceCharacter)
        {
            length++;
        }

        return length >= minimumLength;
    }

    private static void MaskMatches(string source, char[] buffer, Regex expression)
    {
        foreach (Match match in expression.Matches(source))
        {
            MaskRange(buffer, match.Index, match.Length);
        }
    }

    private static void MaskLinks(string source, char[] buffer, Regex expression)
    {
        foreach (Match match in expression.Matches(source))
        {
            MaskRange(buffer, match.Index, match.Length);
            var visibleText = match.Groups[1];
            source.CopyTo(visibleText.Index, buffer, visibleText.Index, visibleText.Length);
        }
    }

    private static void MaskRange(char[] buffer, int start, int length)
    {
        var end = Math.Min(buffer.Length, start + length);
        for (var index = start; index < end; index++)
        {
            if (buffer[index] is not ('\r' or '\n'))
            {
                buffer[index] = ' ';
            }
        }
    }

    [GeneratedRegex(@"^\s{0,3}\[[^\]\r\n]+\]:", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex ReferenceDefinitionExpression();

    [GeneratedRegex(@"`+[^`\r\n]*`+", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex InlineCodeExpression();

    [GeneratedRegex(@"!?\[([^\]\r\n]*)\]\([^\r\n)]*\)", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex InlineLinkExpression();

    [GeneratedRegex(@"!?\[([^\]\r\n]*)\]\[[^\]\r\n]*\]", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex ReferenceLinkExpression();

    [GeneratedRegex(@"<https?://[^>\r\n]+>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 1000)]
    private static partial Regex AutolinkExpression();

    [GeneratedRegex(@"https?://\S+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 1000)]
    private static partial Regex RawUrlExpression();

    [GeneratedRegex(@"<!--[\s\S]*?-->", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex HtmlCommentExpression();

    [GeneratedRegex(@"</?[A-Za-z][^>\r\n]*>", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex HtmlTagExpression();
}
