using ARCANet.Abstractions;
using ARCANet.Invoices;
using ARCANet.Taxpayers;

namespace ARCANet;

public sealed class ArcaClient : IArcaClient
{
    public ArcaClient(IInvoiceClient invoices)
        : this(invoices, new UnconfiguredTaxpayerRegistryClient())
    {
    }

    public ArcaClient(IInvoiceClient invoices, ITaxpayerRegistryClient taxpayers)
    {
        Invoices = invoices ?? throw new ArgumentNullException(nameof(invoices));
        Taxpayers = taxpayers ?? throw new ArgumentNullException(nameof(taxpayers));
    }

    public IInvoiceClient Invoices { get; }

    public ITaxpayerRegistryClient Taxpayers { get; }

    private sealed class UnconfiguredTaxpayerRegistryClient : ITaxpayerRegistryClient
    {
        public Task<TaxpayerProfile?> GetTaxpayerAsync(long taxpayerCuit, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException(
                "Taxpayer registry client is not configured. Use the ArcaClient constructor that receives an ITaxpayerRegistryClient.");
    }
}
