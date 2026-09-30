namespace TextProcessor.Lib;

/// <summary>Состояние прогресса обработки файла.</summary>
public sealed record TextProcessingProgress(
    string FilePath,
    long ProcessedBytes,
    long TotalBytes,
    double SpeedBytesPerSecond)
{
    public double Percent => TotalBytes <= 0 ? 0 : Math.Min(100, ProcessedBytes * 100d / TotalBytes);
}
