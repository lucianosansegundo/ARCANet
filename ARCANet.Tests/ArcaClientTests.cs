using ARCANet.Abstractions;
using ARCANet.Invoices;
using ARCANet.Taxpayers;

namespace ARCANet.Tests;

public sealed class ArcaClientTests
{
    [Fact]
    public void Constructor_ExposesInvoicesAndTaxpayers()
    {
        var invoices = new FakeInvoiceClient();
        var taxpayers = new FakeTaxpayerRegistryClient();

        var client = new ArcaClient(invoices, taxpayers);

        Assert.Same(invoices, client.Invoices);
        Assert.Same(taxpayers, client.Taxpayers);
    }

    [Fact]
    public async Task Taxpayers_WhenNotConfigured_ThrowsHelpfulError()
    {
        var client = new ArcaClient(new FakeInvoiceClient());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.Taxpayers.GetTaxpayerAsync(30712345678));

        Assert.Contains("Taxpayer registry client is not configured", exception.Message, StringComparison.Ordinal);
    }

    private sealed class FakeTaxpayerRegistryClient : ITaxpayerRegistryClient
    {
        public Task<TaxpayerProfile?> GetTaxpayerAsync(long taxpayerCuit, CancellationToken cancellationToken = default) =>
            Task.FromResult<TaxpayerProfile?>(new TaxpayerProfile { Cuit = taxpayerCuit });
    }

    private sealed class FakeInvoiceClient : IInvoiceClient
    {
        public Task<CreateInvoiceResult> CreateInvoiceAsync(
            CreateInvoiceRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<AuthorizedInvoice?> GetInvoiceAsync(
            InvoiceLocator locator,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<long?> GetLastAuthorizedNumberAsync(
            VoucherSeries series,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<InvoiceValidationResult> ValidateCreateInvoiceAsync(
            CreateInvoiceRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }
}
