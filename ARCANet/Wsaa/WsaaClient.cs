using ARCANet.Abstractions;
using ARCANet.Authentication;
using ARCANet.Transport;

namespace ARCANet.Wsaa;

internal sealed class WsaaClient
{
    private const string LoginCmsSoapAction = "http://wsaa.view.sua.dvadac.desein.afip.gov/loginCms";

    private readonly ICertificateProvider _certificateProvider;
    private readonly IArcaSoapTransport _transport;
    private readonly LoginTicketRequestBuilder _requestBuilder;
    private readonly CmsTicketSigner _signer = new();
    private readonly WsaaLoginResponseParser _parser = new();
    private readonly Uri _endpoint;

    public WsaaClient(
        ICertificateProvider certificateProvider,
        IArcaSoapTransport transport,
        IClock clock,
        WsaaOptions options)
    {
        _certificateProvider = certificateProvider ?? throw new ArgumentNullException(nameof(certificateProvider));
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));

        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(options);

        _requestBuilder = new LoginTicketRequestBuilder(clock, options);
        _endpoint = WsaaEndpointResolver.Resolve(options);
    }

    public async Task<AccessTicket> LoginAsync(string service, CancellationToken cancellationToken = default)
    {
        var certificate = await _certificateProvider.GetCertificateAsync(cancellationToken).ConfigureAwait(false);
        var xml = _requestBuilder.BuildXml(service);
        var cms = _signer.Sign(xml, certificate);
        var envelope = WsaaSoapEnvelopeBuilder.BuildLoginCmsEnvelope(cms);

        var response = await _transport.SendAsync(
            new ArcaSoapRequest(_endpoint, LoginCmsSoapAction, envelope),
            cancellationToken).ConfigureAwait(false);

        return _parser.Parse(response);
    }
}
