using TextProcessor.Lib;

namespace TextProcessor.Tests;

public sealed class FileNameTemplateTests
{
    [Fact]
    public void BuildsConfiguredOutputName()
    {
        var options = new TextProcessingOptions
        {
            OutputFileNameTemplate = "{original}_clean{ext}",
            OutputFileSuffix = "_ignored"
        };

        Assert.Equal("data_clean.txt", FileNameTemplate.Build(@"C:\data\data.txt", options));
    }
}