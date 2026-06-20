using ARCANet.Invoices;
using Xunit.Abstractions;

namespace ARCANet.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class HomologationSmokeTests(
    HomologationFixture fixture,
    ITestOutputHelper output) : IClassFixture<HomologationFixture>
{
    private readonly HomologationFixture _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
    private readonly ITestOutputHelper _output = output ?? throw new ArgumentNullException(nameof(output));

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

    [HomologationFact]
    public async Task TaxpayerRegistryClient_GetTaxpayerAsync_ReturnsTaxpayerData()
    {
        var settings = _fixture.Settings;

        var taxpayer = await RetryTaxpayerRegistryTransientAsync(
            () => _fixture.TaxpayerRegistryClient.GetTaxpayerAsync(settings.TaxpayerCuit));

        Assert.NotNull(taxpayer);
        Assert.Equal(settings.TaxpayerCuit, taxpayer!.Cuit);
        Assert.False(string.IsNullOrWhiteSpace(taxpayer.DisplayName));

        _output.WriteLine($"CUIT: {taxpayer.Cuit}");
        _output.WriteLine($"DisplayName: {taxpayer.DisplayName}");
        _output.WriteLine($"PersonType: {taxpayer.PersonType ?? "(none)"}");
        _output.WriteLine($"KeyStatus: {taxpayer.KeyStatus ?? "(none)"}");
        _output.WriteLine($"VatStatus: {taxpayer.VatStatus}");
        _output.WriteLine($"SuggestedReceiverVatCondition: {FormatReceiverVatCondition(taxpayer.SuggestedReceiverVatCondition)}");

        foreach (var tax in taxpayer.GeneralTaxes)
        {
            _output.WriteLine($"GeneralTax: {tax.Id} | {tax.Description} | State={tax.State ?? "(none)"} | Period={tax.Period?.ToString() ?? "(none)"}");
        }

        if (taxpayer.Monotributo is not null)
        {
            _output.WriteLine($"Monotributo: {taxpayer.Monotributo.Tax.Id} | {taxpayer.Monotributo.Tax.Description} | Category={taxpayer.Monotributo.CategoryName ?? "(none)"} | CategoryId={taxpayer.Monotributo.CategoryId?.ToString() ?? "(none)"}");
        }

        foreach (var error in taxpayer.RegistryErrors)
        {
            _output.WriteLine($"RegistryError: {error}");
        }
    }

    private static string FormatReceiverVatCondition(ReceiverVatCondition? condition) =>
        condition is null
            ? "(none)"
            : $"{condition.Id} | {condition.Name}";

    private async Task<T> RetryTaxpayerRegistryTransientAsync<T>(Func<Task<T>> action)
    {
        var delays = new[]
        {
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(20)
        };
        var maxAttempts = delays.Length + 1;

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await action().ConfigureAwait(false);
            }
            catch (Exception exception) when (IsTransientTaxpayerRegistryFailure(exception) && attempt < maxAttempts)
            {
                var delay = delays[attempt - 1];
                _output.WriteLine($"Taxpayer registry attempt {attempt}/{maxAttempts} failed with {exception.GetType().Name}: {exception.Message}");
                _output.WriteLine($"Retrying in {delay.TotalSeconds:0} seconds...");
                await Task.Delay(delay).ConfigureAwait(false);
            }
        }
    }

    private static bool IsTransientTaxpayerRegistryFailure(Exception exception) =>
        exception is HttpRequestException or TaskCanceledException or TimeoutException;

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
