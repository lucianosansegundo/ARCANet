using ARCANet.Configuration;

namespace ARCANet.Wsaa;

internal static class WsaaEndpointResolver
{
    private static readonly Uri HomologationEndpoint = new("https://wsaahomo.afip.gov.ar/ws/services/LoginCms");
    private static readonly Uri ProductionEndpoint = new("https://wsaa.afip.gov.ar/ws/services/LoginCms");

    public static Uri Resolve(WsaaOptions options)
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
