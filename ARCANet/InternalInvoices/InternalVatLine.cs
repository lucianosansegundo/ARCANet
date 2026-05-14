namespace ARCANet.InternalInvoices;

internal sealed record InternalVatLine(
    int Id,
    decimal BaseAmount,
    decimal Rate,
    decimal Amount);
