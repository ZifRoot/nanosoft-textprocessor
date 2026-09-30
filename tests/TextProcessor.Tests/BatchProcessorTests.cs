using TextProcessor.Lib;

namespace TextProcessor.Tests;

public sealed class BatchProcessorTests
{
    [Fact]
    public async Task ProcessesSeveralFilesAndGeneratesDefaultOutputNames()
    {
        var directory = Directory.CreateTempSubdirectory();
        try
        {
            var first = Path.Combine(directory.FullName, "one.txt");
            var second = Path.Combine(directory.FullName, "two.txt");
            await File.WriteAllTextAsync(first, "one four");
            await File.WriteAllTextAsync(second, "two five");

            var result = await new BatchProcessor().ProcessAsync(
                new[]
                {
                    new FileProcessingJob(first, null),
                    new FileProcessingJob(second, null)
                },
                new TextProcessingOptions { MinWordLength = 4 },
                2);

            Assert.Equal(2, result.SuccessfulCount);
            Assert.Equal("four", await File.ReadAllTextAsync(Path.Combine(directory.FullName, "one_processed.txt")));
            Assert.Equal("five", await File.ReadAllTextAsync(Path.Combine(directory.FullName, "two_processed.txt")));
        }
        finally
        {
            directory.Delete(true);
        }
    }
}