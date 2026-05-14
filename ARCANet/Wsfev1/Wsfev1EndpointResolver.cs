using ARCANet.Configuration;

namespace ARCANet.Wsfev1;

internal static class Wsfev1EndpointResolver
{
    private static readonly Uri HomologationEndpoint = new("https://wswhomo.afip.gov.ar/wsfev1/service.asmx");
    private static readonly Uri ProductionEndpoint = new("https://servicios1.afip.gov.ar/wsfev1/service.asmx");

    public static Uri Resolve(Wsfev1Options options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return options.Endpoint ?? options.Environment switch
        {
            ArcaEnvironment.Homologation => HomologationEndpoint,
            ArcaEnvironment.Production => ProductionEndpoint,
            _ => throw new ArgumentOutOfRangeException(nameof(options), options.Environment, "Unknown ARCA environment.")
        };
    }
}
