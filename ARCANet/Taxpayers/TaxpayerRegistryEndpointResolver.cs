using ARCANet.Configuration;

namespace ARCANet.Taxpayers;

internal static class TaxpayerRegistryEndpointResolver
{
    private static readonly Uri HomologationEndpoint = new("https://awshomo.afip.gov.ar/sr-padron/webservices/personaServiceA5");
    private static readonly Uri ProductionEndpoint = new("https://aws.afip.gov.ar/sr-padron/webservices/personaServiceA5");

    public static Uri Resolve(TaxpayerRegistryOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.Endpoint is not null)
        {
            return options.Endpoint;
        }

        return options.Environment switch
        {
            ArcaEnvironment.Homologation => HomologationEndpoint,
            ArcaEnvironment.Production => ProductionEndpoint,
            _ => throw new ArgumentOutOfRangeException(nameof(options), options.Environment, "Unsupported ARCA environment.")
        };
    }
}
