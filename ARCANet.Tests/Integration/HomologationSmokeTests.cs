using ARCANet.Abstractions;
using ARCANet.Invoices;
using ARCANet.Qr;
using ARCANet.Transport;
using ARCANet.Wsaa;
using ARCANet.Wsfev1;

namespace ARCANet.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class HomologationSmokeTests
{
    [HomologationFact]
    public async Task WsaaAccessTicketProvider_ReturnsWsfeAccessTicket()
    {
        var settings = HomologationTestSettings.Load();
        using var httpClient = CreateHttpClient(settings);
        var transport = new HttpClientSoapTransport(httpClient);
        var provider = new WsaaAccessTicketProvider(
            new PfxCertificateProvider(settings.CertificatePath, settings.CertificatePassword),
            transport,
            new SystemClock(),
            new WsaaOptions());

        var ticket = await provider.GetAccessTicketAsync("wsfe");

        Assert.False(string.IsNullOrWhiteSpace(ticket.Token));
        Assert.False(string.IsNullOrWhiteSpace(ticket.Sign));
        Assert.True(ticket.ExpiresAtUtc > DateTimeOffset.UtcNow);
    }

    [HomologationFact]
    public async Task InvoiceClient_GetLastAuthorizedNumberAsync_ReturnsSeriesState()
    {
        var settings = HomologationTestSettings.Load();
        using var httpClient = CreateHttpClient(settings);
        var client = CreateInvoiceClient(settings, httpClient);
        var series = new VoucherSeries(
            settings.Cuit,
            settings.PointOfSale,
            new VoucherType(settings.VoucherTypeCode, settings.VoucherTypeName));

        var lastAuthorized = await client.GetLastAuthorizedNumberAsync(series);

        Assert.NotNull(lastAuthorized);
        Assert.True(lastAuthorized >= 0);
    }

    [HomologationExistingVoucherFact]
    public async Task InvoiceClient_GetInvoiceAsync_ReturnsKnownVoucher()
    {
        var settings = HomologationTestSettings.Load();
        using var httpClient = CreateHttpClient(settings);
        var client = CreateInvoiceClient(settings, httpClient);
        var locator = new InvoiceLocator(
            new VoucherSeries(
                settings.Cuit,
                settings.PointOfSale,
                new VoucherType(settings.VoucherTypeCode, settings.VoucherTypeName)),
            settings.ExistingVoucherNumber!.Value);

        var invoice = await client.GetInvoiceAsync(locator);

        Assert.NotNull(invoice);
        Assert.Equal(settings.Cuit, invoice!.IssuerCuit);
        Assert.Equal(settings.PointOfSale, invoice.Series.PointOfSale);
        Assert.Equal(settings.ExistingVoucherNumber.Value, invoice.VoucherNumber);
        Assert.False(string.IsNullOrWhiteSpace(invoice.AuthorizationCode));
    }

    private static InvoiceClient CreateInvoiceClient(HomologationTestSettings settings, HttpClient httpClient)
    {
        IArcaSoapTransport transport = new HttpClientSoapTransport(httpClient);
        var accessTicketProvider = new WsaaAccessTicketProvider(
            new PfxCertificateProvider(settings.CertificatePath, settings.CertificatePassword),
            transport,
            new SystemClock(),
            new WsaaOptions());

        return new InvoiceClient(
            new InvoiceRequestValidator(new SystemClock()),
            new ArcaQrGenerator(),
            accessTicketProvider,
            transport,
            new Wsfev1Options());
    }

    private static HttpClient CreateHttpClient(HomologationTestSettings settings) =>
        new()
        {
            Timeout = settings.HttpTimeout
        };
}
