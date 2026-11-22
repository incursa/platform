using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Incursa.Prose.Cli;

internal static partial class Readability
{
    public static ProseMetrics Measure(string visibleText, IReadOnlyList<TextToken> tokens)
    {
        ArgumentNullException.ThrowIfNull(visibleText);
        ArgumentNullException.ThrowIfNull(tokens);

        var wordCount = tokens.Count;
        if (wordCount == 0)
        {
            return new ProseMetrics(0, 0, 0, null, null);
        }

        var sentenceCount = Math.Max(1, SentenceEndExpression().Count(visibleText));
        var syllableCount = tokens.Sum(token => SyllableCounter.Count(token.Value));
        return Calculate(sentenceCount, wordCount, syllableCount);
    }

    internal static ProseMetrics Calculate(int sentences, int words, int syllables)
    {
        if (sentences <= 0 || words <= 0 || syllables <= 0)
        {
            return new ProseMetrics(sentences, words, syllables, null, null);
        }

        var wordsPerSentence = (double)words / sentences;
        var syllablesPerWord = (double)syllables / words;
        var readingEase = 206.835 - (1.015 * wordsPerSentence) - (84.6 * syllablesPerWord);
        var gradeLevel = (0.39 * wordsPerSentence) + (11.8 * syllablesPerWord) - 15.59;
        return new ProseMetrics(sentences, words, syllables, readingEase, gradeLevel);
    }

    [GeneratedRegex(@"[.!?]+(?:[\""')\]]+)?(?=\s|$)", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex SentenceEndExpression();
}

internal static partial class SyllableCounter
{
    private static readonly Dictionary<string, int> Problematic = new(StringComparer.Ordinal)
    {
        ["abalone"] = 4,
        ["abare"] = 3,
        ["abbruzzese"] = 4,
        ["abed"] = 2,
        ["aborigine"] = 5,
        ["abruzzese"] = 4,
        ["acreage"] = 3,
        ["adame"] = 3,
        ["adieu"] = 2,
        ["adobe"] = 3,
        ["anemone"] = 4,
        ["anyone"] = 3,
        ["apache"] = 3,
        ["aphrodite"] = 4,
        ["apostrophe"] = 4,
        ["ariadne"] = 4,
        ["cafe"] = 2,
        ["calliope"] = 4,
        ["catastrophe"] = 4,
        ["chile"] = 2,
        ["chloe"] = 2,
        ["circe"] = 2,
        ["coyote"] = 3,
        ["daphne"] = 2,
        ["epitome"] = 4,
        ["eurydice"] = 4,
        ["euterpe"] = 3,
        ["every"] = 2,
        ["everywhere"] = 3,
        ["forever"] = 3,
        ["gethsemane"] = 4,
        ["guacamole"] = 4,
        ["hermione"] = 4,
        ["hyperbole"] = 4,
        ["jesse"] = 2,
        ["jukebox"] = 2,
        ["karate"] = 3,
        ["machete"] = 3,
        ["maybe"] = 2,
        ["naive"] = 2,
        ["newlywed"] = 3,
        ["penelope"] = 4,
        ["people"] = 2,
        ["persephone"] = 4,
        ["phoebe"] = 2,
        ["pulse"] = 1,
        ["queue"] = 1,
        ["recipe"] = 3,
        ["riverbed"] = 3,
        ["sesame"] = 3,
        ["shoreline"] = 2,
        ["simile"] = 3,
        ["snuffleupagus"] = 5,
        ["sometimes"] = 2,
        ["syncope"] = 3,
        ["tamale"] = 3,
        ["waterbed"] = 3,
        ["wednesday"] = 2,
        ["yosemite"] = 4,
        ["zoe"] = 2,
    };

    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "The pinned syllable algorithm uses lowercase ASCII regular expressions.")]
    public static int Count(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var normalized = RemoveDiacritics(value).ToLowerInvariant().Replace("'", string.Empty, StringComparison.Ordinal);
        return AlphaSequenceExpression().Matches(normalized).Sum(match => CountWord(match.Value));
    }

    private static int CountWord(string value)
    {
        if (value.Length == 0)
        {
            return 0;
        }

        if (value.Length < 3)
        {
            return 1;
        }

        if (TryGetProblematic(value, out var knownCount))
        {
            return knownCount;
        }

        var count = 0;
        value = ReplaceAndCount(value, TripleExpression(), 3, ref count);
        value = ReplaceAndCount(value, DoubleExpression(), 2, ref count);
        value = ReplaceAndCount(value, SingleExpression(), 1, ref count);
        count += VowelGroupExpression().Count(value);
        count -= MonosyllabicOneExpression().Count(value);
        count -= MonosyllabicTwoExpression().Count(value);
        count += DoubleSyllabicOneExpression().Count(value);
        count += DoubleSyllabicTwoExpression().Count(value);
        count += DoubleSyllabicThreeExpression().Count(value);
        count += DoubleSyllabicFourExpression().Count(value);
        return Math.Max(1, count);
    }

    private static string ReplaceAndCount(string value, Regex expression, int addition, ref int count)
    {
        count += expression.Count(value) * addition;
        return expression.Replace(value, string.Empty);
    }

    private static bool TryGetProblematic(string value, out int count)
    {
        if (Problematic.TryGetValue(value, out count))
        {
            return true;
        }

        foreach (var singular in GetSingularCandidates(value))
        {
            if (Problematic.TryGetValue(singular, out count))
            {
                return true;
            }
        }

        count = 0;
        return false;
    }

    private static IEnumerable<string> GetSingularCandidates(string value)
    {
        if (value.EndsWith("ies", StringComparison.Ordinal) && value.Length > 3)
        {
            yield return value[..^3] + "y";
        }

        if (value.EndsWith("es", StringComparison.Ordinal) && value.Length > 2)
        {
            yield return value[..^2];
            yield return value[..^1];
        }
        else if (value.EndsWith('s') && value.Length > 1)
        {
            yield return value[..^1];
        }
    }

    private static string RemoveDiacritics(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    [GeneratedRegex("[a-z]+", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex AlphaSequenceExpression();

    [GeneratedRegex("[aeiouy]+", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex VowelGroupExpression();

    [GeneratedRegex(@"(creations?|ology|ologist|onomy|onomist)$", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex TripleExpression();

    [GeneratedRegex(@"^(?:above|anti|ante|counter|hyper|afore|agri|infra|intra|inter|over|semi|ultra|under|extra|dia|micro|mega|kilo|pico|nano|macro|somer)|(?:fully|berry|woman|women|edly|union|((?:[bcdfghjklmnpqrstvwxz])|[aeiou])ye?ing)$", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex DoubleExpression();

    [GeneratedRegex(@"^(?:un|fore|ware|none?|out|post|sub|pre|pro|dis|side|some)|(?:ly|less|some|ful|ers?|ness|cians?|ments?|ettes?|villes?|ships?|sides?|ports?|shires?|[gnst]ion(?:ed|s)?)$", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex SingleExpression();

    [GeneratedRegex(@"awe($|d|so)|cia(?:l|$)|tia|cius|cious|[^aeiou]giu|[aeiouy][^aeiouy]ion|iou|sia$|eous$|[oa]gue$|.[^aeiuoycgltdb]{2,}ed$|.ely$|^jua|uai|eau|^busi$|(?:[aeiouy](?:[bcfgklmnprsvwxyz]|ch|dg|g[hn]|lch|l[lv]|mm|nch|n[cgn]|r[bcnsv]|squ|s[chkls]|th)ed$)|(?:[aeiouy](?:[bdfklmnprstvy]|ch|g[hn]|lch|l[lv]|mm|nch|nn|r[nsv]|squ|s[cklst]|th)es$)", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex MonosyllabicOneExpression();

    [GeneratedRegex(@"[aeiouy](?:[bcdfgklmnprstvyz]|ch|dg|g[hn]|l[lv]|mm|n[cgns]|r[cnsv]|squ|s[cklst]|th)e$", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex MonosyllabicTwoExpression();

    [GeneratedRegex(@"(?:([^aeiouy])\1l|[^aeiouy]ie(?:r|s?t)|[aeiouym]bl|eo|ism|asm|thm|dnt|snt|uity|dea|gean|oa|ua|react?|orbed|shred|eings?|[aeiouy]sh?e[rs])$", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex DoubleSyllabicOneExpression();

    [GeneratedRegex(@"creat(?!u)|[^gq]ua[^auieo]|[aeiou]{3}|^(?:ia|mc|coa[dglx].)|^re(app|es|im|us)|(th|d)eist", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex DoubleSyllabicTwoExpression();

    [GeneratedRegex(@"[^aeiou]y[ae]|[^l]lien|riet|dien|iu|io|ii|uen|[aeilotu]real|real[aeilotu]|iell|eo[^aeiou]|[aeiou]y[aeiou]", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex DoubleSyllabicThreeExpression();

    [GeneratedRegex(@"[^s]ia", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex DoubleSyllabicFourExpression();
}
