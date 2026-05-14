namespace ARCANet.Invoices;

public sealed record UnknownInvoiceResult(
    InvoiceAttempt Attempt,
    string Reason,
    bool ShouldQueryBeforeRetry) : CreateInvoiceResult;
