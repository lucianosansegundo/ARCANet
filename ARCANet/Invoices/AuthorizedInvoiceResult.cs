namespace ARCANet.Invoices;

public sealed record AuthorizedInvoiceResult(
    AuthorizedInvoice Invoice,
    IReadOnlyList<InvoiceObservation> Observations) : CreateInvoiceResult;
