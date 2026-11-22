namespace Incursa.Prose.Cli.Tests;

public sealed class ProseAnalyzerTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void Analyze_ReportsMergedCategoriesAndLongestPhrases()
    {
        var lexicon = Lexicon.Create(
        [
            ("basically", ProseCategory.Filler),
            ("basically", ProseCategory.Hedge),
            ("paradigm", ProseCategory.Buzzword),
            ("paradigm shift", ProseCategory.Buzzword),
        ]);
        var analyzer = new ProseAnalyzer(lexicon);

        var result = analyzer.Analyze("sample.txt", "Basically, this is a paradigm shift.");

        result.Findings.Count.ShouldBe(2);
        result.Findings[0].Line.ShouldBe(1);
        result.Findings[0].Column.ShouldBe(1);
        result.Findings[0].Categories.ShouldBe([ProseCategory.Filler, ProseCategory.Hedge]);
        result.Findings[1].Text.ShouldBe("paradigm shift");
        result.Findings[1].Term.ShouldBe("paradigm shift");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Analyze_DoesNotJoinPhraseAcrossPunctuation()
    {
        var lexicon = Lexicon.Create([("paradigm shift", ProseCategory.Buzzword)]);
        var analyzer = new ProseAnalyzer(lexicon);

        var result = analyzer.Analyze("sample.txt", "A paradigm. Shift the wording.");

        result.Findings.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Analyze_MasksMarkdownSyntaxAndKeepsVisibleLinkText()
    {
        var lexicon = Lexicon.Create([("basically", ProseCategory.Filler)]);
        var analyzer = new ProseAnalyzer(lexicon);
        const string Markdown = """
            ---
            title: basically hidden
            ---
            `basically hidden`

            ```text
            basically hidden
            ```

            <!-- basically hidden -->

            [basically visible](https://example.test/basically-hidden)
            """;

        var result = analyzer.Analyze("sample.md", Markdown);

        result.Findings.Count.ShouldBe(1);
        result.Findings[0].Line.ShouldBe(12);
        result.Findings[0].Column.ShouldBe(2);
        result.Findings[0].Text.ShouldBe("basically");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Analyze_RequiresLiteralTrailingHyphenInLexiconTerm()
    {
        var lexicon = Lexicon.Create([("bio-", ProseCategory.Buzzword)]);
        var analyzer = new ProseAnalyzer(lexicon);

        var result = analyzer.Analyze("sample.txt", "Biology is not bio-.");

        result.Findings.Count.ShouldBe(1);
        result.Findings[0].Text.ShouldBe("bio-");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Analyze_UsesAdvisoryDefaultLexicon()
    {
        var analyzer = new ProseAnalyzer(Lexicon.Default);

        var result = analyzer.Analyze("sample.txt", "This is basically probably a paradigm shift.");

        result.Findings.ShouldContain(finding => finding.Term == "basically" && finding.Categories.Contains(ProseCategory.Filler));
        result.Findings.ShouldContain(finding => finding.Term == "probably" && finding.Categories.Contains(ProseCategory.Hedge));
        result.Findings.ShouldContain(finding => finding.Term == "paradigm shift" && finding.Categories.Contains(ProseCategory.Buzzword));
    }
}
