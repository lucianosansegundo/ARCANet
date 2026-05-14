using ARCANet.Abstractions;
using ARCANet.Authentication;
using ARCANet.InternalInvoices;
using ARCANet.Invoices;
using ARCANet.Transport;

namespace ARCANet.Wsfev1;

internal sealed class Wsfev1Client
{
    private const string ServiceName = "wsfe";
    private const string SoapActionBase = "http://ar.gov.afip.dif.FEV1/";

    private readonly IAccessTicketProvider _accessTicketProvider;
    private readonly IArcaSoapTransport _transport;
    private readonly WsfeSoapEnvelopeBuilder _envelopeBuilder;
    private readonly WsfeSoapResponseParser _parser = new();
    private readonly Uri _endpoint;

    public Wsfev1Client(
        IAccessTicketProvider accessTicketProvider,
        IArcaSoapTransport transport,
        Wsfev1Options options)
    {
        _accessTicketProvider = accessTicketProvider ?? throw new ArgumentNullException(nameof(accessTicketProvider));
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));

        ArgumentNullException.ThrowIfNull(options);
        _envelopeBuilder = new WsfeSoapEnvelopeBuilder(options);
        _endpoint = Wsfev1EndpointResolver.Resolve(options);
    }

    public async Task<long?> GetLastAuthorizedNumberAsync(VoucherSeries series, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(series);

        var ticket = await GetAccessTicketAsync(cancellationToken).ConfigureAwait(false);
        var body = _envelopeBuilder.BuildFeCompUltimoAutorizado(ticket, series.IssuerCuit, series.PointOfSale, series.VoucherType.Code);
        var response = await SendAsync($"{SoapActionBase}FECompUltimoAutorizado", body, cancellationToken).ConfigureAwait(false);
        return _parser.ParseLastAuthorizedNumber(response);
    }

    public async Task<WsfeInvoiceRecord?> GetInvoiceAsync(InvoiceLocator locator, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(locator);

        var series = locator.Series;
        var ticket = await GetAccessTicketAsync(cancellationToken).ConfigureAwait(false);
        var body = _envelopeBuilder.BuildFeCompConsultar(ticket, series.IssuerCuit, series.PointOfSale, series.VoucherType.Code, locator.VoucherNumber);
        var response = await SendAsync($"{SoapActionBase}FECompConsultar", body, cancellationToken).ConfigureAwait(false);
        return _parser.ParseCompConsultar(response);
    }

    public async Task<WsfeAuthorizationResponse> AuthorizeAsync(InternalInvoiceSubmission submission, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(submission);

        var ticket = await GetAccessTicketAsync(cancellationToken).ConfigureAwait(false);
        var body = _envelopeBuilder.BuildFeCaeSolicitar(ticket, submission);
        var response = await SendAsync($"{SoapActionBase}FECAESolicitar", body, cancellationToken).ConfigureAwait(false);
        return _parser.ParseFeCaeSolicitar(response);
    }

    private Task<AccessTicket> GetAccessTicketAsync(CancellationToken cancellationToken) =>
        _accessTicketProvider.GetAccessTicketAsync(ServiceName, cancellationToken);

    private Task<string> SendAsync(string soapAction, string body, CancellationToken cancellationToken) =>
        _transport.SendAsync(new ArcaSoapRequest(_endpoint, soapAction, body), cancellationToken);
}
