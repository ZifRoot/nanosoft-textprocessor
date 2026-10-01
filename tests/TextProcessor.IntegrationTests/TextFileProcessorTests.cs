using TextProcessor.Lib;

namespace TextProcessor.IntegrationTests;

public sealed class TextFileProcessorTests
{
    [Fact]
    public async Task ProcessesFileWithoutLoadingWholeFileIntoStrategy()
    {
        var directory = Directory.CreateTempSubdirectory();
        try
        {
            var input = Path.Combine(directory.FullName, "input.txt");
            var output = Path.Combine(directory.FullName, "output.txt");
            await File.WriteAllTextAsync(input, "one two three\nfour five six");

            await new TextFileProcessor().ProcessAsync(input, output,
                new LengthBasedRemovalStrategy(new TextProcessingOptions { MinWordLength = 4 }));

            Assert.Equal("three" + Environment.NewLine + "four five six", await File.ReadAllTextAsync(output));
        }
        finally { directory.Delete(true); }
    }

    [Fact]
    public async Task ReplacesInputAtomicallyWhenPathsMatch()
    {
        var directory = Directory.CreateTempSubdirectory();
        try
        {
            var input = Path.Combine(directory.FullName, "input.txt");
            await File.WriteAllTextAsync(input, "one four");

            await new TextFileProcessor().ProcessAsync(input, input,
                new LengthBasedRemovalStrategy(new TextProcessingOptions { MinWordLength = 4 }),
                atomicReplace: true);

            Assert.Equal("four", await File.ReadAllTextAsync(input));
        }
        finally { directory.Delete(true); }
    }

    [Fact]
    public async Task CancelsBeforeOpeningFile()
    {
        var directory = Directory.CreateTempSubdirectory();
        try
        {
            var input = Path.Combine(directory.FullName, "input.txt");
            var output = Path.Combine(directory.FullName, "output.txt");
            await File.WriteAllTextAsync(input, "some content");

            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                new TextFileProcessor().ProcessAsync(input, output,
                    new LengthBasedRemovalStrategy(new TextProcessingOptions { MinWordLength = 2 }),
                    cancellationToken: cancellation.Token));
        }
        finally { directory.Delete(true); }
    }
}
