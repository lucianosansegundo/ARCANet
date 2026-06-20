using ARCANet.Abstractions;
using ARCANet.Invoices;

namespace ARCANet;

public sealed class ArcaClient(IInvoiceClient invoices) : IArcaClient
{
    public IInvoiceClient Invoices { get; } = invoices ?? throw new ArgumentNullException(nameof(invoices));
}
