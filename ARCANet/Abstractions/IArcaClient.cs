using ARCANet.Invoices;

namespace ARCANet.Abstractions;

public interface IArcaClient
{
    IInvoiceClient Invoices { get; }
}
