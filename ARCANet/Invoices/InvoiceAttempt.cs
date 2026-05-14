namespace ARCANet.Invoices;

public sealed record InvoiceAttempt
{
    public required long IssuerCuit { get; init; }
    public required VoucherSeries Series { get; init; }
    public required long VoucherNumber { get; init; }
    public required DateOnly IssueDate { get; init; }
    public required CustomerIdentity Customer { get; init; }
    public required ReceiverVatCondition ReceiverVatCondition { get; init; }
    public required MoneyTotals Totals { get; init; }
    public required CurrencyAmount Currency { get; init; }
    public string? ExternalIdempotencyKey { get; init; }
}
