namespace ARCANet.Invoices;

public sealed record VoucherSeries(
    long IssuerCuit,
    int PointOfSale,
    VoucherType VoucherType);
