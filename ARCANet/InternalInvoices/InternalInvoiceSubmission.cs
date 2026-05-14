using ARCANet.Invoices;

namespace ARCANet.InternalInvoices;

internal sealed record InternalInvoiceSubmission
{
    public required long IssuerCuit { get; init; }
    public required VoucherSeries Series { get; init; }
    public required long VoucherNumber { get; init; }
    public required InvoiceConcept Concept { get; init; }
    public required DateOnly IssueDate { get; init; }
    public DateOnly? ServiceFrom { get; init; }
    public DateOnly? ServiceTo { get; init; }
    public DateOnly? PaymentDueDate { get; init; }
    public required InternalInvoiceReceiver Receiver { get; init; }
    public required CurrencyAmount Currency { get; init; }
    public required InternalInvoiceTotals Totals { get; init; }
    public IReadOnlyList<InternalVatLine> VatLines { get; init; } = [];
    public IReadOnlyList<InternalTributeLine> TributeLines { get; init; } = [];
    public IReadOnlyList<InternalAssociatedVoucher> AssociatedVouchers { get; init; } = [];
    public string? ExternalIdempotencyKey { get; init; }
}
