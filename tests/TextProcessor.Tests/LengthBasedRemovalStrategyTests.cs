using TextProcessor.Lib;

namespace TextProcessor.Tests;

public sealed class LengthBasedRemovalStrategyTests
{
    [Fact]
    public void RemovesWordsShorterThanMinimumLength()
    {
        var strategy = new LengthBasedRemovalStrategy(new TextProcessingOptions
        {
            MinWordLength = 4,
            RemovePunctuation = false,
            WhitespaceNormalization = WhitespaceNormalization.None
        });

        Assert.Equal("four words", strategy.Process("a four abc words"));
    }

    [Theory]
    [InlineData(PunctuationHandling.Remove, "Приветмир")]
    [InlineData(PunctuationHandling.ReplaceWithSpace, "Привет мир")]
    [InlineData(PunctuationHandling.SmartReplace, "Привет мир")]
    public void AppliesPunctuationMode(PunctuationHandling mode, string expected)
    {
        var strategy = new LengthBasedRemovalStrategy(new TextProcessingOptions
        {
            MinWordLength = 1,
            PunctuationHandling = mode,
            WhitespaceNormalization = WhitespaceNormalization.None
        });

        Assert.Equal(expected, strategy.Process("Привет,мир"));
    }

    [Fact]
    public void KeepsConfiguredWordCharacters()
    {
        var strategy = new LengthBasedRemovalStrategy(new TextProcessingOptions
        {
            MinWordLength = 5,
            KeepDigitsAsPartOfWord = true,
            KeepHyphenAsPartOfWord = true,
            KeepApostropheAsPartOfWord = true,
            AdditionalWordChars = "_@",
            RemovePunctuation = false,
            WhitespaceNormalization = WhitespaceNormalization.None
        });

        Assert.Equal("abc-123 O’Connor user_name @home", strategy.Process("abc-123 O’Connor user_name @home"));
    }

    [Fact]
    public void NormalizesWhitespaceAndPreservesLineBreaks()
    {
        var strategy = new LengthBasedRemovalStrategy(new TextProcessingOptions
        {
            MinWordLength = 1,
            RemovePunctuation = false,
            WhitespaceNormalization = WhitespaceNormalization.Collapse,
            PreserveLineBreaks = true
        });

        Assert.Equal("one two\nthree four", strategy.Process("one   two\nthree\t four"));
    }

    [Fact]
    public void TrimsWhitespaceWhenRequested()
    {
        var strategy = new LengthBasedRemovalStrategy(new TextProcessingOptions
        {
            MinWordLength = 1,
            RemovePunctuation = false,
            WhitespaceNormalization = WhitespaceNormalization.Trim
        });

        Assert.Equal("one two", strategy.Process("  one   two  "));
    }
}