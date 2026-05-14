using ARCANet.Qr;

namespace ARCANet.Invoices;

public sealed record AuthorizedInvoice
{
    public required long IssuerCuit { get; init; }
    public required VoucherSeries Series { get; init; }
    public required long VoucherNumber { get; init; }
    public required DateOnly IssueDate { get; init; }
    public required InvoiceConcept Concept { get; init; }
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
    public required AuthorizationCodeType AuthorizationCodeType { get; init; }
    public required string AuthorizationCode { get; init; }
    public required DateOnly AuthorizationDueDate { get; init; }
    public required DateTimeOffset ProcessedAtUtc { get; init; }
    public ArcaQrPayload? QrPayload { get; init; }
    public Uri? QrUrl { get; init; }
}
