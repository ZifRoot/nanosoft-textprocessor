using System.Text;

namespace TextProcessor.Lib;

/// <summary>Потоково обрабатывает текстовый файл.</summary>
public sealed class TextFileProcessor
{
    private const int BufferSize = 64 * 1024;

    public async Task ProcessAsync(
        string inputPath,
        string outputPath,
        ITextProcessingStrategy strategy,
        TextEncodingKind encodingKind = TextEncodingKind.Auto,
        CancellationToken cancellationToken = default,
        IProgress<TextProcessingProgress>? progress = null,
        bool atomicReplace = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(strategy);

        if (!File.Exists(inputPath))
            throw new FileNotFoundException("Входной файл не найден.", inputPath);

        var inputFullPath = Path.GetFullPath(inputPath);
        var outputFullPath = Path.GetFullPath(outputPath);
        var sameFile = string.Equals(inputFullPath, outputFullPath, StringComparison.OrdinalIgnoreCase);

        if (sameFile && !atomicReplace)
            throw new InvalidOperationException("Входной и выходной файлы совпадают. Требуется безопасный режим.");

        var directory = Path.GetDirectoryName(outputFullPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var targetPath = sameFile ? CreateTemporaryPath(outputFullPath) : outputFullPath;
        var encoding = EncodingResolver.Resolve(encodingKind, inputFullPath);
        var totalBytes = new FileInfo(inputFullPath).Length;
        var started = Stopwatch.StartNew();

        try
        {
            await using var input = new FileStream(inputFullPath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, useAsync: true);
            await using var output = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, useAsync: true);
            using var reader = new StreamReader(input, encoding, detectEncodingFromByteOrderMarks: true, bufferSize: BufferSize, leaveOpen: true);
            await using var writer = new StreamWriter(output, encoding, BufferSize, leaveOpen: true);

            var buffer = new char[BufferSize];
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var count = await reader.ReadAsync(buffer.AsMemory(), cancellationToken);
                if (count == 0)
                    break;

                var chunk = new string(buffer, 0, count);
                await writer.WriteAsync(strategy.Process(chunk));
                var processed = input.Position;
                progress?.Report(new TextProcessingProgress(
                    inputPath,
                    Math.Min(processed, totalBytes),
                    totalBytes,
                    Math.Min(processed, totalBytes) / Math.Max(started.Elapsed.TotalSeconds, 0.001)));
            }

            await writer.FlushAsync(cancellationToken);

            if (sameFile)
            {
                File.Move(targetPath, outputFullPath, overwrite: true);
            }
        }
        catch
        {
            if (sameFile && File.Exists(targetPath))
                File.Delete(targetPath);
            throw;
        }
    }

    private static string CreateTemporaryPath(string outputPath) =>
        outputPath + "." + Guid.NewGuid().ToString("N") + ".tmp";

    private static System.Diagnostics.Stopwatch Stopwatch => System.Diagnostics.Stopwatch.StartNew();
}
