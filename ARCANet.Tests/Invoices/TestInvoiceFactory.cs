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

    public static AuthorizedInvoice CreateAuthorizedFacturaA() =>
        new()
        {
            IssuerCuit = 20304050607,
            Series = new VoucherSeries(
                20304050607,
                5,
                new VoucherType(1, "Factura A")),
            VoucherNumber = 1234,
            IssueDate = new DateOnly(2026, 5, 14),
            Concept = InvoiceConcept.Products,
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
            ],
            AuthorizationCodeType = AuthorizationCodeType.Cae,
            AuthorizationCode = "12345678901234",
            AuthorizationDueDate = new DateOnly(2026, 6, 1),
            ProcessedAtUtc = new DateTimeOffset(2026, 5, 14, 14, 30, 0, TimeSpan.Zero)
        };

    public static AuthorizedInvoice CreateAuthorizedCreditNoteB() =>
        new()
        {
            IssuerCuit = 20304050607,
            Series = new VoucherSeries(
                20304050607,
                5,
                new VoucherType(8, "Nota de Credito B", VoucherKind.CreditNote)),
            VoucherNumber = 88,
            IssueDate = new DateOnly(2026, 5, 18),
            Concept = InvoiceConcept.Products,
            Customer = new CustomerIdentity
            {
                Name = "Consumidor Final",
                IsConsumerFinal = true
            },
            ReceiverVatCondition = new ReceiverVatCondition(5, "Consumidor Final"),
            Totals = new MoneyTotals
            {
                TotalAmount = 100.00m,
                TaxableAmount = 100.00m
            },
            Currency = new CurrencyAmount("PES", 1.00m),
            AssociatedVouchers =
            [
                new AssociatedVoucher
                {
                    VoucherType = new VoucherType(6, "Factura B"),
                    PointOfSale = 5,
                    VoucherNumber = 4321,
                    IssuedOn = new DateOnly(2026, 5, 10)
                }
            ],
            AuthorizationCodeType = AuthorizationCodeType.Cae,
            AuthorizationCode = "12345678901235",
            AuthorizationDueDate = new DateOnly(2026, 6, 2),
            ProcessedAtUtc = new DateTimeOffset(2026, 5, 18, 17, 45, 0, TimeSpan.Zero)
        };
}
