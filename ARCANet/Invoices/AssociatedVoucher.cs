namespace ARCANet.Invoices;

public sealed record AssociatedVoucher
{
    public required VoucherType VoucherType { get; init; }
    public required int PointOfSale { get; init; }
    public required long VoucherNumber { get; init; }
    public long? IssuerCuit { get; init; }
    public DateOnly? IssuedOn { get; init; }
}
