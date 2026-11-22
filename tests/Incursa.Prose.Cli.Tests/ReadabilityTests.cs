namespace Incursa.Prose.Cli.Tests;

public sealed class ReadabilityTests
{
    [Theory]
    [Trait("Category", "Unit")]
    [InlineData("syllable", 3)]
    [InlineData("unicorn", 3)]
    [InlineData("hi", 1)]
    [InlineData("hihi", 2)]
    [InlineData("mmmmmmmmmmmmmmmm", 1)]
    [InlineData("wine", 1)]
    [InlineData("bottle", 2)]
    [InlineData("wine-bottle", 3)]
    [InlineData("Åland", 2)]
    public void SyllableCounter_MatchesUpstreamExamples(string word, int expected)
    {
        SyllableCounter.Count(word).ShouldBe(expected);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Calculate_MatchesPinnedFleschFormulaExamples()
    {
        var metrics = Readability.Calculate(sentences: 1, words: 13, syllables: 26);

        metrics.FleschReadingEase.ShouldNotBeNull();
        Math.Round(metrics.FleschReadingEase.Value, 6).ShouldBe(24.44);
        metrics.FleschKincaidGradeLevel.ShouldNotBeNull();
        Math.Round(metrics.FleschKincaidGradeLevel.Value, 6).ShouldBe(13.08);
    }
}
