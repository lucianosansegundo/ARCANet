namespace ARCANet.Invoices;

public sealed record VoucherType(
    int Code,
    string Name,
    VoucherKind Kind = VoucherKind.Invoice);
