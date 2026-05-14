using ARCANet.Configuration;

namespace ARCANet.Wsaa;

public sealed record WsaaOptions
{
    public ArcaEnvironment Environment { get; init; } = ArcaEnvironment.Homologation;

    public Uri? Endpoint { get; init; }

    public TimeSpan GenerationTimeOffset { get; init; } = TimeSpan.FromMinutes(-10);

    public TimeSpan RequestLifetime { get; init; } = TimeSpan.FromMinutes(20);

    public TimeSpan RefreshBeforeExpiration { get; init; } = TimeSpan.FromMinutes(5);
}
