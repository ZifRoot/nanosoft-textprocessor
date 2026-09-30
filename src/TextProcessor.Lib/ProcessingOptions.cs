namespace TextProcessor.Lib;

/// <summary>Настройки обработки текстовых файлов.</summary>
public sealed class TextProcessingOptions
{
    public int MinWordLength { get; init; } = 3;
    public bool RemovePunctuation { get; init; } = true;
    public HashSet<char> PunctuationSet { get; init; } = new(".,!?;:—…()[]{}\"'–-");
    public PunctuationHandling PunctuationHandling { get; init; } = PunctuationHandling.SmartReplace;
    public bool KeepDigitsAsPartOfWord { get; init; } = true;
    public bool KeepHyphenAsPartOfWord { get; init; } = true;
    public bool KeepApostropheAsPartOfWord { get; init; } = true;
    public string AdditionalWordChars { get; init; } = string.Empty;
    public WhitespaceNormalization WhitespaceNormalization { get; init; } = WhitespaceNormalization.Collapse;
    public bool PreserveLineBreaks { get; init; } = true;
    public string OutputFileNameTemplate { get; init; } = "{original}{suffix}{ext}";
    public string OutputFileSuffix { get; init; } = "_processed";

    public void Validate()
    {
        if (MinWordLength < 1 || MinWordLength > 100)
            throw new ArgumentOutOfRangeException(nameof(MinWordLength), "Длина слова должна быть от 1 до 100.");
        if (string.IsNullOrWhiteSpace(OutputFileNameTemplate))
            throw new ArgumentException("Шаблон имени файла не может быть пустым.", nameof(OutputFileNameTemplate));
    }
}

public enum PunctuationHandling { Remove, ReplaceWithSpace, SmartReplace }
public enum WhitespaceNormalization { None, Collapse, Trim }
public enum TextEncodingKind { Auto, Utf8, Utf16LE, Utf16BE, Windows1251, Ascii }
