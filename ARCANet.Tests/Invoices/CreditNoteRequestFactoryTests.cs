using ARCANet.Invoices;
using ARCANet.Qr;

namespace ARCANet.Tests.Invoices;

public sealed class CreditNoteRequestFactoryTests
{
    [Fact]
    public void CreateFullCancellation_FromFacturaA_MapsExpectedCreditNoteRequest()
    {
        var original = CreateFacturaA();

        var request = CreditNoteRequestFactory.CreateFullCancellation(
            original,
            voucherNumber: 4321,
            issueDate: new DateOnly(2026, 5, 17),
            externalIdempotencyKey: "refund-1");

        Assert.Equal(original.IssuerCuit, request.IssuerCuit);
        Assert.Equal(3, request.VoucherType.Code);
        Assert.Equal("Nota de Credito A", request.VoucherType.Name);
        Assert.Equal(VoucherKind.CreditNote, request.VoucherType.Kind);
        Assert.Equal(original.Series.PointOfSale, request.PointOfSale);
        Assert.Equal(4321, request.VoucherNumber);
        Assert.Equal(new DateOnly(2026, 5, 17), request.IssueDate);
        Assert.Equal(original.Customer, request.Customer);
        Assert.Equal(original.ReceiverVatCondition, request.ReceiverVatCondition);
        Assert.Equal(original.Totals, request.Totals);
        Assert.Equal(original.Currency, request.Currency);
        Assert.Equal(original.VatItems, request.VatItems);
        Assert.Equal(original.Tributes, request.Tributes);
        Assert.Equal("refund-1", request.ExternalIdempotencyKey);
        Assert.Single(request.AssociatedVouchers);
        Assert.Equal(original.Series.VoucherType, request.AssociatedVouchers[0].VoucherType);
        Assert.Equal(original.VoucherNumber, request.AssociatedVouchers[0].VoucherNumber);
        Assert.Equal(original.IssueDate, request.AssociatedVouchers[0].IssuedOn);
    }

    [Fact]
    public void CreateFullCancellation_FromFacturaB_MapsExpectedCreditNoteType()
    {
        var original = CreateFacturaB();

        var request = CreditNoteRequestFactory.CreateFullCancellation(
            original,
            voucherNumber: 99,
            issueDate: new DateOnly(2026, 5, 17));

        Assert.Equal(8, request.VoucherType.Code);
        Assert.Equal("Nota de Credito B", request.VoucherType.Name);
        Assert.Equal(VoucherKind.CreditNote, request.VoucherType.Kind);
    }

    [Fact]
    public void CreatePartial_UsesSuppliedTotalsAndLines()
    {
        var original = CreateFacturaA();
        var partialTotals = new MoneyTotals
        {
            TotalAmount = 605.00m,
            TaxableAmount = 500.00m,
            VatAmount = 105.00m
        };
        var partialVatItems = new[]
        {
            new VatItem
            {
                Id = 5,
                BaseAmount = 500.00m,
                Rate = 21.00m,
                Amount = 105.00m
            }
        };

        var request = CreditNoteRequestFactory.CreatePartial(
            original,
            voucherNumber: 777,
            issueDate: new DateOnly(2026, 5, 17),
            totals: partialTotals,
            vatItems: partialVatItems);

        Assert.Equal(3, request.VoucherType.Code);
        Assert.Equal(partialTotals, request.Totals);
        Assert.Equal(partialVatItems, request.VatItems);
        Assert.Single(request.AssociatedVouchers);
        Assert.Equal(original.VoucherNumber, request.AssociatedVouchers[0].VoucherNumber);
    }

    [Fact]
    public void CreatePartial_Throws_WhenPartialTotalsExceedOriginal()
    {
        var original = CreateFacturaA();
        var invalidTotals = new MoneyTotals
        {
            TotalAmount = 1300.00m,
            TaxableAmount = 1000.00m,
            VatAmount = 300.00m
        };
        var invalidVatItems = new[]
        {
            new VatItem
            {
                Id = 5,
                BaseAmount = 1000.00m,
                Rate = 30.00m,
                Amount = 300.00m
            }
        };

        var exception = Assert.Throws<InvalidOperationException>(() =>
            CreditNoteRequestFactory.CreatePartial(
                original,
                voucherNumber: 778,
                issueDate: new DateOnly(2026, 5, 17),
                totals: invalidTotals,
                vatItems: invalidVatItems));

        Assert.Contains(nameof(MoneyTotals.TotalAmount), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CreatePartial_Throws_WhenVatBreakdownDoesNotMatchTotals()
    {
        var original = CreateFacturaA();
        var partialTotals = new MoneyTotals
        {
            TotalAmount = 605.00m,
            TaxableAmount = 500.00m,
            VatAmount = 105.00m
        };
        var invalidVatItems = new[]
        {
            new VatItem
            {
                Id = 5,
                BaseAmount = 500.00m,
                Rate = 21.00m,
                Amount = 100.00m
            }
        };

        var exception = Assert.Throws<InvalidOperationException>(() =>
            CreditNoteRequestFactory.CreatePartial(
                original,
                voucherNumber: 779,
                issueDate: new DateOnly(2026, 5, 17),
                totals: partialTotals,
                vatItems: invalidVatItems));

        Assert.Contains("VAT amount", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CreateFullCancellation_Throws_ForUnsupportedOriginalVoucherType()
    {
        var original = CreateFacturaA() with
        {
            Series = new VoucherSeries(CreateFacturaA().IssuerCuit, 5, new VoucherType(11, "Factura C"))
        };

        var exception = Assert.Throws<InvalidOperationException>(() =>
            CreditNoteRequestFactory.CreateFullCancellation(
                original,
                voucherNumber: 100,
                issueDate: new DateOnly(2026, 5, 17)));

        Assert.Contains("only supported", exception.Message, StringComparison.Ordinal);
    }

    private static AuthorizedInvoice CreateFacturaA() =>
        new()
        {
            IssuerCuit = 20304050607,
            Series = new VoucherSeries(20304050607, 5, new VoucherType(1, "Factura A")),
            VoucherNumber = 1234,
            IssueDate = new DateOnly(2026, 5, 16),
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
            AuthorizationCode = "70417054367476",
            AuthorizationDueDate = new DateOnly(2026, 5, 26),
            ProcessedAtUtc = new DateTimeOffset(2026, 5, 16, 12, 0, 0, TimeSpan.Zero),
            QrPayload = CreateQrPayload(20304050607, 5, 1, 1234, 1210.00m, 80, 30712345678),
            QrUrl = new Uri("https://www.arca.gob.ar/fe/qr/?p=test")
        };

    private static AuthorizedInvoice CreateFacturaB() =>
        new()
        {
            IssuerCuit = 20304050607,
            Series = new VoucherSeries(20304050607, 5, new VoucherType(6, "Factura B")),
            VoucherNumber = 1235,
            IssueDate = new DateOnly(2026, 5, 16),
            Concept = InvoiceConcept.Products,
            Customer = new CustomerIdentity
            {
                Name = "Consumidor Final",
                IsConsumerFinal = true
            },
            ReceiverVatCondition = new ReceiverVatCondition(5, "Consumidor Final"),
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
            AuthorizationCode = "70417054367477",
            AuthorizationDueDate = new DateOnly(2026, 5, 26),
            ProcessedAtUtc = new DateTimeOffset(2026, 5, 16, 12, 0, 0, TimeSpan.Zero),
            QrPayload = CreateQrPayload(20304050607, 5, 6, 1235, 1210.00m, 99, 0),
            QrUrl = new Uri("https://www.arca.gob.ar/fe/qr/?p=test")
        };

    private static ArcaQrPayload CreateQrPayload(
        long issuerCuit,
        int pointOfSale,
        int voucherTypeCode,
        long voucherNumber,
        decimal totalAmount,
        int receiverDocumentTypeCode,
        long receiverDocumentNumber) =>
        new()
        {
            Version = 1,
            IssueDate = new DateOnly(2026, 5, 16),
            IssuerCuit = issuerCuit,
            PointOfSale = pointOfSale,
            VoucherTypeCode = voucherTypeCode,
            VoucherNumber = voucherNumber,
            TotalAmount = totalAmount,
            CurrencyCode = "PES",
            CurrencyExchangeRate = 1.00m,
            ReceiverDocumentTypeCode = receiverDocumentTypeCode,
            ReceiverDocumentNumber = receiverDocumentNumber,
            AuthorizationCodeType = "E",
            AuthorizationCode = 70417054367476
        };
}
