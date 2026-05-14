namespace ARCANet.InternalInvoices;

internal sealed record InternalTributeLine(
    int Id,
    string? Description,
    decimal BaseAmount,
    decimal Rate,
    decimal Amount);
