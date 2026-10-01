using System.Text;

namespace TextProcessor.Lib;

/// <summary>Удаляет слова короче заданной длины и, при необходимости, пунктуацию.</summary>
public sealed class LengthBasedRemovalStrategy : ITextProcessingStrategy
{
    private readonly TextProcessingOptions _options;

    public LengthBasedRemovalStrategy(TextProcessingOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _options.Validate();
    }

    public string Process(string input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var output = new StringBuilder(input.Length);
        var word = new StringBuilder();
        var pendingWhitespace = false;

        void AppendPendingWhitespace()
        {
            if (pendingWhitespace && output.Length > 0 && output[^1] is not '\r' and not '\n')
            {
                output.Append(' ');
            }

            pendingWhitespace = false;
        }

        void FlushWord()
        {
            if (word.Length >= _options.MinWordLength)
            {
                AppendPendingWhitespace();
                output.Append(word);
            }

            word.Clear();
        }

        for (var index = 0; index < input.Length; index++)
        {
            var current = input[index];

            if (IsWordCharacter(current))
            {
                word.Append(current);
                continue;
            }

            FlushWord();

            if (_options.RemovePunctuation && _options.PunctuationSet.Contains(current))
            {
                if (_options.PunctuationHandling == PunctuationHandling.ReplaceWithSpace ||
                    (_options.PunctuationHandling == PunctuationHandling.SmartReplace &&
                     HasWordCharacterBefore(input, index) && HasWordCharacterAfter(input, index)))
                {
                    pendingWhitespace = true;
                }

                continue;
            }

            if (char.IsWhiteSpace(current))
            {
                if (_options.PreserveLineBreaks && current is '\r' or '\n')
                {
                    pendingWhitespace = false;

                    if (output.Length > 0 && output[^1] is not '\r' and not '\n')
                    {
                        output.Append(current);
                    }
                    else if (output.Length > 0 && current == '\n' && output[^1] == '\r')
                    {
                        output.Append(current);
                    }

                    continue;
                }

                pendingWhitespace = true;
                continue;
            }

            AppendPendingWhitespace();
            output.Append(current);
        }

        FlushWord();
        return NormalizeWhitespace(output.ToString());
    }

    private bool IsWordCharacter(char value)
    {
        if (char.IsLetter(value))
            return true;
        if (char.IsDigit(value))
            return _options.KeepDigitsAsPartOfWord;
        if (_options.KeepHyphenAsPartOfWord && value == '-')
            return true;
        if (_options.KeepApostropheAsPartOfWord && (value == '\'' || value == '’'))
            return true;
        return _options.AdditionalWordChars.Contains(value);
    }

    private bool HasWordCharacterBefore(string text, int index)
    {
        return index > 0 && IsLetterOrDigit(text[index - 1]);
    }

    private bool HasWordCharacterAfter(string text, int index)
    {
        return index + 1 < text.Length && IsLetterOrDigit(text[index + 1]);
    }

    private static bool IsLetterOrDigit(char value) => char.IsLetterOrDigit(value);

    private string NormalizeWhitespace(string value)
    {
        if (_options.WhitespaceNormalization == WhitespaceNormalization.None)
            return value;

        var output = new StringBuilder(value.Length);
        var whitespace = false;

        foreach (var current in value)
        {
            if (current is '\r' or '\n')
            {
                if (_options.PreserveLineBreaks)
                {
                    whitespace = false;
                    output.Append(current);
                    continue;
                }
            }

            if (char.IsWhiteSpace(current))
            {
                if (!whitespace)
                    output.Append(' ');
                whitespace = true;
                continue;
            }

            whitespace = false;
            output.Append(current);
        }

        var result = output.ToString();
        return _options.WhitespaceNormalization == WhitespaceNormalization.Trim
            ? result.Trim()
            : result;
    }
}
