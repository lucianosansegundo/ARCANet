using ARCANet.Abstractions;
using ARCANet.Authentication;
using ARCANet.Transport;
using System.Security.Cryptography.X509Certificates;

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

    public Task<AccessTicket> LoginAsync(string service, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(service);
        return LoginWithCertificateAsync(
            service,
            _certificateProvider.GetCertificateAsync(cancellationToken),
            cancellationToken);
    }

    public Task<AccessTicket> LoginAsync(
        string service,
        X509Certificate2 certificate,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(service);
        ArgumentNullException.ThrowIfNull(certificate);

        return LoginWithCertificateAsync(service, Task.FromResult(certificate), cancellationToken);
    }

    private async Task<AccessTicket> LoginWithCertificateAsync(
        string service,
        Task<X509Certificate2> certificateTask,
        CancellationToken cancellationToken)
    {
        var certificate = await certificateTask.ConfigureAwait(false);
        var xml = _requestBuilder.BuildXml(service);
        var cms = _signer.Sign(xml, certificate);
        var envelope = WsaaSoapEnvelopeBuilder.BuildLoginCmsEnvelope(cms);

        var response = await _transport.SendAsync(
            new ArcaSoapRequest(_endpoint, LoginCmsSoapAction, envelope),
            cancellationToken).ConfigureAwait(false);

        return _parser.Parse(response);
    }
}
