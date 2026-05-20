using ARCANet.Configuration;

namespace ARCANet.Taxpayers;

public sealed record TaxpayerRegistryOptions
{
    public ArcaEnvironment Environment { get; init; } = ArcaEnvironment.Homologation;

    public Uri? Endpoint { get; init; }
}
