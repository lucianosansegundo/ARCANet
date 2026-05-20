namespace ARCANet.Taxpayers;

public sealed record TaxpayerTax(
    long Id,
    string Description,
    string? State,
    int? Period);
