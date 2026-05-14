namespace ARCANet.Invoices;

public sealed record RejectedInvoiceResult(
    InvoiceAttempt Attempt,
    IReadOnlyList<InvoiceRejection> Rejections,
    IReadOnlyList<InvoiceObservation> Observations) : CreateInvoiceResult;
