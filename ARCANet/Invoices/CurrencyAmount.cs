namespace ARCANet.Invoices;

public sealed record CurrencyAmount(
    string Code,
    decimal ExchangeRate);
