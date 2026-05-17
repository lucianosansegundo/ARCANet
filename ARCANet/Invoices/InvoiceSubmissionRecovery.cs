using ARCANet.Abstractions;

namespace ARCANet.Invoices;

public sealed class InvoiceSubmissionRecovery(IInvoiceClient invoiceClient)
{
    private readonly IInvoiceClient _invoiceClient = invoiceClient ?? throw new ArgumentNullException(nameof(invoiceClient));

    public Task<InvoiceReconciliationResult> ReconcileAsync(
        UnknownInvoiceResult result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);
        return ReconcileAsync(result.Attempt, cancellationToken);
    }

    public async Task<InvoiceReconciliationResult> ReconcileAsync(
        InvoiceAttempt attempt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(attempt);

        var locator = new InvoiceLocator(attempt.Series, attempt.VoucherNumber);
        var invoice = await _invoiceClient.GetInvoiceAsync(locator, cancellationToken).ConfigureAwait(false);

        if (invoice is not null)
        {
            return new AuthorizedInvoiceReconciliationResult
            {
                Attempt = attempt,
                Invoice = invoice
            };
        }

        return new UnconfirmedInvoiceReconciliationResult
        {
            Attempt = attempt
        };
    }
}
