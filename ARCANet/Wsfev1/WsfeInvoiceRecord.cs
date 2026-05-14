using ARCANet.Invoices;

namespace ARCANet.Wsfev1;

internal sealed record WsfeInvoiceRecord
{
    public required long IssuerCuit { get; init; }
    public required int PointOfSale { get; init; }
    public required int VoucherTypeCode { get; init; }
    public required long VoucherNumber { get; init; }
    public required string Result { get; init; }
    public required string AuthorizationCode { get; init; }
    public required DateOnly IssueDate { get; init; }
    public required InvoiceConcept Concept { get; init; }
    public required int CustomerDocumentTypeCode { get; init; }
    public required string CustomerDocumentNumber { get; init; }
    public DateOnly? ServiceFrom { get; init; }
    public DateOnly? ServiceTo { get; init; }
    public DateOnly? PaymentDueDate { get; init; }
    public required MoneyTotals Totals { get; init; }
    public required CurrencyAmount Currency { get; init; }
    public IReadOnlyList<VatItem> VatItems { get; init; } = [];
    public IReadOnlyList<TributeItem> Tributes { get; init; } = [];
    public IReadOnlyList<AssociatedVoucher> AssociatedVouchers { get; init; } = [];
    public required DateOnly AuthorizationDueDate { get; init; }
    public required DateTimeOffset ProcessedAtUtc { get; init; }
    public required string EmissionType { get; init; }
    public IReadOnlyList<WsfeResultIssue> Observations { get; init; } = [];
    public IReadOnlyList<WsfeResultIssue> Events { get; init; } = [];
    public IReadOnlyList<WsfeResultIssue> Errors { get; init; } = [];
}
