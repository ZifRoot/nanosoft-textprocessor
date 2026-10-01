namespace TextProcessor.Lib;

/// <summary>Выполняет пакетную обработку файлов с ограничением параллелизма.</summary>
public sealed class BatchProcessor
{
    private readonly TextFileProcessor _fileProcessor;

    public BatchProcessor(TextFileProcessor? fileProcessor = null)
    {
        _fileProcessor = fileProcessor ?? new TextFileProcessor();
    }

    public async Task<BatchResult> ProcessAsync(
        IReadOnlyCollection<FileProcessingJob> jobs,
        TextProcessingOptions options,
        int maxDegreeOfParallelism,
        CancellationToken cancellationToken = default,
        IProgress<TextProcessingProgress>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(jobs);
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        if (maxDegreeOfParallelism < 1)
            throw new ArgumentOutOfRangeException(nameof(maxDegreeOfParallelism));

        var jobSizes = jobs.ToDictionary(
            job => Path.GetFullPath(job.InputPath),
            job => new FileInfo(job.InputPath).Length,
            StringComparer.OrdinalIgnoreCase);
        var processedByFile = new System.Collections.Concurrent.ConcurrentDictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        var totalBytes = jobSizes.Values.Sum();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var results = new System.Collections.Concurrent.ConcurrentBag<FileProcessingResult>();

        await Parallel.ForEachAsync(
            jobs,
            new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism, CancellationToken = cancellationToken },
            async (job, token) =>
            {
                try
                {
                    var output = string.IsNullOrWhiteSpace(job.OutputPath)
                        ? Path.Combine(
                            Path.GetDirectoryName(Path.GetFullPath(job.InputPath)) ?? string.Empty,
                            FileNameTemplate.Build(job.InputPath, options))
                        : job.OutputPath;

                    var inputFull = Path.GetFullPath(job.InputPath);
                    var outputFull = Path.GetFullPath(output);
                    var same = string.Equals(inputFull, outputFull, StringComparison.OrdinalIgnoreCase);

                    var fileProgress = new Progress<TextProcessingProgress>(value =>
                    {
                        processedByFile[value.FilePath] = value.ProcessedBytes;
                        var processedTotal = processedByFile.Values.Sum();
                        progress?.Report(new TextProcessingProgress(
                            value.FilePath,
                            processedTotal,
                            totalBytes,
                            processedTotal / Math.Max(stopwatch.Elapsed.TotalSeconds, 0.001)));
                    });

                    await _fileProcessor.ProcessAsync(
                        job.InputPath,
                        output,
                        new LengthBasedRemovalStrategy(options),
                        job.Encoding,
                        token,
                        fileProgress,
                        same);

                    processedByFile[inputFull] = jobSizes[inputFull];
                    results.Add(new FileProcessingResult(job.InputPath, output, true, null));
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    results.Add(new FileProcessingResult(job.InputPath, job.OutputPath, false, exception.Message));
                }
            });

        return new BatchResult(results.OrderBy(x => x.InputPath, StringComparer.OrdinalIgnoreCase).ToArray());
    }
}

public sealed record FileProcessingJob(string InputPath, string? OutputPath, TextEncodingKind Encoding = TextEncodingKind.Auto);
public sealed record FileProcessingResult(string InputPath, string? OutputPath, bool Success, string? Error);
public sealed record BatchResult(IReadOnlyList<FileProcessingResult> Results)
{
    public int SuccessfulCount => Results.Count(x => x.Success);
    public int FailedCount => Results.Count(x => !x.Success);
}
