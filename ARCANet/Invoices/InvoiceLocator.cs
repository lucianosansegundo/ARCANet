namespace ARCANet.Invoices;

public sealed record InvoiceLocator(
    VoucherSeries Series,
    long VoucherNumber);
