namespace ARCANet.Invoices;

public sealed record VatItem
{
    public required int Id { get; init; }
    public required decimal BaseAmount { get; init; }
    public required decimal Rate { get; init; }
    public required decimal Amount { get; init; }
}
