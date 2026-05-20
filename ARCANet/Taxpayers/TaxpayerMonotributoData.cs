namespace ARCANet.Taxpayers;

public sealed record TaxpayerMonotributoData(
    TaxpayerTax Tax,
    string? CategoryName,
    long? CategoryId);
