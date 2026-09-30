using System.Text;

namespace TextProcessor.Lib;

/// <summary>Создаёт кодировки и определяет кодировку по BOM.</summary>
public static class EncodingResolver
{
    static EncodingResolver()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public static Encoding Resolve(TextEncodingKind kind, string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (kind != TextEncodingKind.Auto)
            return Create(kind);

        using var stream = File.OpenRead(path);
        Span<byte> bom = stackalloc byte[4];
        var read = stream.Read(bom);
        if (read >= 3 && bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF)
            return new UTF8Encoding(true);
        if (read >= 2 && bom[0] == 0xFF && bom[1] == 0xFE)
            return Encoding.Unicode;
        if (read >= 2 && bom[0] == 0xFE && bom[1] == 0xFF)
            return Encoding.BigEndianUnicode;

        return new UTF8Encoding(false, true);
    }

    private static Encoding Create(TextEncodingKind kind) => kind switch
    {
        TextEncodingKind.Utf8 => new UTF8Encoding(false, true),
        TextEncodingKind.Utf16LE => new UnicodeEncoding(false, true, true),
        TextEncodingKind.Utf16BE => new UnicodeEncoding(true, true, true),
        TextEncodingKind.Windows1251 => Encoding.GetEncoding(1251, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback),
        TextEncodingKind.Ascii => new ASCIIEncoding(),
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
}
