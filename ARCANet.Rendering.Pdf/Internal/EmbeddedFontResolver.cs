using System.Reflection;
using PdfSharp.Fonts;

namespace ARCANet.Rendering.Pdf.Internal;

internal sealed class EmbeddedFontResolver : IFontResolver
{
    public const string SansRegular = "ARCANet.Lato.Regular";
    public const string SansBold = "ARCANet.Lato.Bold";
    public const string SansItalic = "ARCANet.Lato.Italic";
    public const string SansBoldItalic = "ARCANet.Lato.BoldItalic";

    private static readonly Assembly Assembly = typeof(EmbeddedFontResolver).Assembly;
    private static readonly Lazy<IReadOnlyDictionary<string, byte[]>> FontData = new(LoadFonts);

    public FontResolverInfo? ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        var normalizedFamily = NormalizeFamilyName(familyName);

        if (normalizedFamily is not ("arial" or "helvetica" or "times new roman" or "times" or "courier new" or "courier" or "lato"))
        {
            normalizedFamily = "lato";
        }

        return new FontResolverInfo(GetFaceName(isBold, isItalic));
    }

    public byte[]? GetFont(string faceName) =>
        FontData.Value.TryGetValue(faceName, out var data)
            ? data
            : null;

    private static string NormalizeFamilyName(string familyName) =>
        string.IsNullOrWhiteSpace(familyName)
            ? "lato"
            : familyName.Trim().ToLowerInvariant();

    private static string GetFaceName(bool isBold, bool isItalic) =>
        (isBold, isItalic) switch
        {
            (true, true) => SansBoldItalic,
            (true, false) => SansBold,
            (false, true) => SansItalic,
            _ => SansRegular
        };

    private static IReadOnlyDictionary<string, byte[]> LoadFonts() =>
        new Dictionary<string, byte[]>
        {
            [SansRegular] = ReadResource("ARCANet.Rendering.Pdf.Fonts.Lato-Regular.ttf"),
            [SansBold] = ReadResource("ARCANet.Rendering.Pdf.Fonts.Lato-Bold.ttf"),
            [SansItalic] = ReadResource("ARCANet.Rendering.Pdf.Fonts.Lato-Italic.ttf"),
            [SansBoldItalic] = ReadResource("ARCANet.Rendering.Pdf.Fonts.Lato-BoldItalic.ttf")
        };

    private static byte[] ReadResource(string resourceName)
    {
        using var stream = Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded font resource '{resourceName}' was not found.");
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }
}
