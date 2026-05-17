namespace ARCANet.Invoices;

public abstract record InvoiceReconciliationResult
{
    public required InvoiceAttempt Attempt { get; init; }
}

public sealed record AuthorizedInvoiceReconciliationResult : InvoiceReconciliationResult
{
    public required AuthorizedInvoice Invoice { get; init; }
}

public sealed record UnconfirmedInvoiceReconciliationResult : InvoiceReconciliationResult
{
    public string Reason { get; init; } = "The invoice could not be confirmed via FECompConsultar.";
}
