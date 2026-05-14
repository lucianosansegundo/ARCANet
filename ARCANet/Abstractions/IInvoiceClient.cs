using ARCANet.Invoices;

namespace ARCANet.Abstractions;

public interface IInvoiceClient
{
    Task<CreateInvoiceResult> CreateInvoiceAsync(
        CreateInvoiceRequest request,
        CancellationToken cancellationToken = default);

    Task<AuthorizedInvoice?> GetInvoiceAsync(
        InvoiceLocator locator,
        CancellationToken cancellationToken = default);

    Task<long?> GetLastAuthorizedNumberAsync(
        VoucherSeries series,
        CancellationToken cancellationToken = default);

    Task<InvoiceValidationResult> ValidateCreateInvoiceAsync(
        CreateInvoiceRequest request,
        CancellationToken cancellationToken = default);
}
