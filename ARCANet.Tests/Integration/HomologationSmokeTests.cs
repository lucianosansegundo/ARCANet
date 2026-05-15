using ARCANet.Invoices;

namespace ARCANet.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class HomologationSmokeTests(HomologationFixture fixture) : IClassFixture<HomologationFixture>
{
    private readonly HomologationFixture _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));

    [HomologationFact]
    public async Task WsaaAccessTicketProvider_ReturnsWsfeAccessTicket()
    {
        var ticket = await _fixture.AccessTicketProvider.GetAccessTicketAsync("wsfe");

        Assert.False(string.IsNullOrWhiteSpace(ticket.Token));
        Assert.False(string.IsNullOrWhiteSpace(ticket.Sign));
        Assert.True(ticket.ExpiresAtUtc > DateTimeOffset.UtcNow);
    }

    [HomologationFact]
    public async Task InvoiceClient_GetLastAuthorizedNumberAsync_ReturnsSeriesState()
    {
        var settings = _fixture.Settings;
        var series = new VoucherSeries(
            settings.Cuit,
            settings.PointOfSale,
            new VoucherType(settings.VoucherTypeCode, settings.VoucherTypeName));

        var lastAuthorized = await _fixture.InvoiceClient.GetLastAuthorizedNumberAsync(series);

        Assert.NotNull(lastAuthorized);
        Assert.True(lastAuthorized >= 0);
    }

    [HomologationExistingVoucherFact]
    public async Task InvoiceClient_GetInvoiceAsync_ReturnsKnownVoucher()
    {
        var settings = _fixture.Settings;
        var locator = new InvoiceLocator(
            new VoucherSeries(
                settings.Cuit,
                settings.PointOfSale,
                new VoucherType(settings.VoucherTypeCode, settings.VoucherTypeName)),
            settings.ExistingVoucherNumber!.Value);

        var invoice = await _fixture.InvoiceClient.GetInvoiceAsync(locator);

        Assert.NotNull(invoice);
        Assert.Equal(settings.Cuit, invoice!.IssuerCuit);
        Assert.Equal(settings.PointOfSale, invoice.Series.PointOfSale);
        Assert.Equal(settings.ExistingVoucherNumber.Value, invoice.VoucherNumber);
        Assert.False(string.IsNullOrWhiteSpace(invoice.AuthorizationCode));
    }
}
