using ARCANet.Abstractions;
using ARCANet.Invoices;
using ARCANet.Qr;
using ARCANet.Transport;
using ARCANet.Wsaa;
using ARCANet.Wsfev1;

namespace ARCANet.Tests.Integration;

public sealed class HomologationFixture : IDisposable
{
    private readonly HttpClient _httpClient;

    public HomologationFixture()
    {
        Settings = HomologationTestSettings.Load();
        _httpClient = new HttpClient
        {
            Timeout = Settings.HttpTimeout
        };

        Transport = new HttpClientSoapTransport(_httpClient);
        AccessTicketProvider = new WsaaAccessTicketProvider(
            new PfxCertificateProvider(Settings.CertificatePath, Settings.CertificatePassword),
            Transport,
            new SystemClock(),
            new WsaaOptions());

        InvoiceClient = new InvoiceClient(
            new InvoiceRequestValidator(new SystemClock()),
            new ArcaQrGenerator(),
            AccessTicketProvider,
            Transport,
            new Wsfev1Options());
    }

    internal HomologationTestSettings Settings { get; }

    public IArcaSoapTransport Transport { get; }

    public WsaaAccessTicketProvider AccessTicketProvider { get; }

    public InvoiceClient InvoiceClient { get; }

    public void Dispose() => _httpClient.Dispose();
}
