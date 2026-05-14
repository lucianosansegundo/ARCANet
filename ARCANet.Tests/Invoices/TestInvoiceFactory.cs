using ARCANet.Invoices;

namespace ARCANet.Tests.Invoices;

internal static class TestInvoiceFactory
{
    public static CreateInvoiceRequest CreateValidFacturaBRequest() =>
        new()
        {
            IssuerCuit = 20304050607,
            VoucherType = new VoucherType(6, "Factura B"),
            PointOfSale = 5,
            VoucherNumber = 1234,
            Concept = InvoiceConcept.Products,
            IssueDate = new DateOnly(2026, 5, 14),
            Customer = new CustomerIdentity
            {
                Name = "Consumidor Final",
                IsConsumerFinal = true
            },
            ReceiverVatCondition = new ReceiverVatCondition(5, "Consumidor Final"),
            Totals = new MoneyTotals
            {
                TotalAmount = 1000.00m,
                TaxableAmount = 1000.00m
            },
            Currency = new CurrencyAmount("PES", 1.00m)
        };

    public static CreateInvoiceRequest CreateValidFacturaARequest() =>
        new()
        {
            IssuerCuit = 20304050607,
            VoucherType = new VoucherType(1, "Factura A"),
            PointOfSale = 5,
            VoucherNumber = 1234,
            Concept = InvoiceConcept.Products,
            IssueDate = new DateOnly(2026, 5, 14),
            Customer = new CustomerIdentity
            {
                Name = "Cliente SA",
                DocumentTypeCode = 80,
                DocumentNumber = "30712345678"
            },
            ReceiverVatCondition = new ReceiverVatCondition(1, "IVA Responsable Inscripto"),
            Totals = new MoneyTotals
            {
                TotalAmount = 1210.00m,
                TaxableAmount = 1000.00m,
                VatAmount = 210.00m
            },
            Currency = new CurrencyAmount("PES", 1.00m),
            VatItems =
            [
                new VatItem
                {
                    Id = 5,
                    BaseAmount = 1000.00m,
                    Rate = 21.00m,
                    Amount = 210.00m
                }
            ]
        };
}
