namespace ARCANet.InternalInvoices;

internal sealed record InternalInvoiceTotals(
    decimal TotalAmount,
    decimal TaxableAmount,
    decimal NonTaxedAmount,
    decimal ExemptAmount,
    decimal VatAmount,
    decimal OtherTaxesAmount);
