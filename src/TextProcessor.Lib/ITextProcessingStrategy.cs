namespace TextProcessor.Lib;

/// <summary>Определяет алгоритм обработки текста.</summary>
public interface ITextProcessingStrategy
{
    string Process(string input);
}
