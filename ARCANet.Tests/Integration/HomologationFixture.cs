using ARCANet.Abstractions;
using ARCANet.Authentication;
using ARCANet.Invoices;
using ARCANet.Qr;
using ARCANet.Taxpayers;
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

        Transport = new RecordingSoapTransport(new HttpClientSoapTransport(_httpClient));
        var accessTicketStore = new FileAccessTicketStore(Settings.AccessTicketStorePath);
        var certificateProvider = new PfxCertificateProvider(Settings.CertificatePath, Settings.CertificatePassword);
        AccessTicketProvider = new WsaaAccessTicketProvider(
            certificateProvider,
            Transport,
            new SystemClock(),
            new WsaaOptions(),
            accessTicketStore);

        InvoiceClient = new InvoiceClient(
            new InvoiceRequestValidator(new SystemClock()),
            new ArcaQrGenerator(),
            AccessTicketProvider,
            Transport,
            new Wsfev1Options());

        TaxpayerRegistryClient = new TaxpayerRegistryClient(
            certificateProvider,
            AccessTicketProvider,
            Transport,
            new TaxpayerRegistryOptions());
    }

    internal HomologationTestSettings Settings { get; }

    public RecordingSoapTransport Transport { get; }

    public WsaaAccessTicketProvider AccessTicketProvider { get; }

    public InvoiceClient InvoiceClient { get; }

    public TaxpayerRegistryClient TaxpayerRegistryClient { get; }

    public void Dispose() => _httpClient.Dispose();
}
