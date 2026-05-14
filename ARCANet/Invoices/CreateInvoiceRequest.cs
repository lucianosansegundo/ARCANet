namespace ARCANet.Invoices;

public sealed record CreateInvoiceRequest
{
    public required long IssuerCuit { get; init; }
    public required VoucherType VoucherType { get; init; }
    public required int PointOfSale { get; init; }
    public required long VoucherNumber { get; init; }
    public required InvoiceConcept Concept { get; init; }
    public required DateOnly IssueDate { get; init; }
    public DateOnly? ServiceFrom { get; init; }
    public DateOnly? ServiceTo { get; init; }
    public DateOnly? PaymentDueDate { get; init; }
    public required CustomerIdentity Customer { get; init; }
    public required ReceiverVatCondition ReceiverVatCondition { get; init; }
    public required MoneyTotals Totals { get; init; }
    public required CurrencyAmount Currency { get; init; }
    public IReadOnlyList<VatItem> VatItems { get; init; } = [];
    public IReadOnlyList<TributeItem> Tributes { get; init; } = [];
    public IReadOnlyList<AssociatedVoucher> AssociatedVouchers { get; init; } = [];
    public string? ExternalIdempotencyKey { get; init; }
}
