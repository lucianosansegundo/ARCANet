using ARCANet.Abstractions;
using ARCANet.Authentication;
using ARCANet.Transport;
using ARCANet.Wsaa;

namespace ARCANet.Taxpayers;

public sealed class TaxpayerRegistryClient : ITaxpayerRegistryClient
{
    private const string ServiceName = "ws_sr_constancia_inscripcion";

    private readonly ICertificateProvider _certificateProvider;
    private readonly IAccessTicketProvider _accessTicketProvider;
    private readonly IArcaSoapTransport _transport;
    private readonly TaxpayerRegistryResponseParser _parser = new();
    private readonly Uri _endpoint;

    public TaxpayerRegistryClient(
        ICertificateProvider certificateProvider,
        IAccessTicketProvider accessTicketProvider,
        IArcaSoapTransport transport,
        TaxpayerRegistryOptions? options = null)
    {
        _certificateProvider = certificateProvider ?? throw new ArgumentNullException(nameof(certificateProvider));
        _accessTicketProvider = accessTicketProvider ?? throw new ArgumentNullException(nameof(accessTicketProvider));
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _endpoint = TaxpayerRegistryEndpointResolver.Resolve(options ?? new TaxpayerRegistryOptions());
    }

    public async Task<TaxpayerProfile?> GetTaxpayerAsync(long taxpayerCuit, CancellationToken cancellationToken = default)
    {
        if (taxpayerCuit is < 10000000000 or > 99999999999)
        {
            throw new ArgumentOutOfRangeException(nameof(taxpayerCuit), "Taxpayer CUIT must contain exactly 11 digits.");
        }

        var representedCuit = await GetRepresentedCuitAsync(cancellationToken).ConfigureAwait(false);
        var ticket = await _accessTicketProvider.GetAccessTicketAsync(ServiceName, cancellationToken).ConfigureAwait(false);
        var envelope = TaxpayerRegistrySoapEnvelopeBuilder.BuildGetPersonaEnvelope(ticket, representedCuit, taxpayerCuit);

        var response = await _transport.SendAsync(
            new ArcaSoapRequest(_endpoint, string.Empty, envelope),
            cancellationToken).ConfigureAwait(false);

        return _parser.ParseGetPersonaResponse(response);
    }

    private async Task<long> GetRepresentedCuitAsync(CancellationToken cancellationToken)
    {
        var certificate = await _certificateProvider.GetCertificateAsync(cancellationToken).ConfigureAwait(false);
        return CertificateIdentityResolver.GetRepresentedCuit(certificate);
    }
}
