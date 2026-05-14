using ARCANet.Configuration;

namespace ARCANet.Wsfev1;

public sealed record Wsfev1Options
{
    public ArcaEnvironment Environment { get; init; } = ArcaEnvironment.Homologation;

    public Uri? Endpoint { get; init; }

    // Configurable technical fallback for consumer final when the caller omits document info.
    public int ConsumerFinalDocumentTypeCode { get; init; } = 99;

    public long ConsumerFinalDocumentNumber { get; init; } = 0;
}
