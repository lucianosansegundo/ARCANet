namespace ARCANet.Qr;

public sealed record ArcaQrOptions
{
    public static Uri DefaultBaseUrl { get; } = new("https://www.arca.gob.ar/fe/qr/");

    public Uri BaseUrl { get; init; } = DefaultBaseUrl;

    public int Version { get; init; } = 1;
}
