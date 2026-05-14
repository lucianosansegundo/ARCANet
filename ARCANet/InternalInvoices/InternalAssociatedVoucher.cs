using ARCANet.Invoices;

namespace ARCANet.InternalInvoices;

internal sealed record InternalAssociatedVoucher(
    VoucherType VoucherType,
    int PointOfSale,
    long VoucherNumber,
    long? IssuerCuit,
    DateOnly? IssuedOn);
