namespace TextProcessor.Lib;

/// <summary>Формирует имя выходного файла по шаблону.</summary>
public static class FileNameTemplate
{
    public static string Build(string inputPath, TextProcessingOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPath);
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        var fileName = Path.GetFileNameWithoutExtension(inputPath);
        var extension = Path.GetExtension(inputPath);
        var result = options.OutputFileNameTemplate
            .Replace("{original}", fileName, StringComparison.Ordinal)
            .Replace("{ext}", extension, StringComparison.Ordinal)
            .Replace("{suffix}", options.OutputFileSuffix, StringComparison.Ordinal);

        if (string.IsNullOrWhiteSpace(result))
            throw new InvalidOperationException("Шаблон сформировал пустое имя файла.");

        return result;
    }
}
