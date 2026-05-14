namespace ARCANet.Invoices;

public sealed record MoneyTotals
{
    public required decimal TotalAmount { get; init; }
    public decimal TaxableAmount { get; init; }
    public decimal NonTaxedAmount { get; init; }
    public decimal ExemptAmount { get; init; }
    public decimal VatAmount { get; init; }
    public decimal OtherTaxesAmount { get; init; }
}
