using ARCANet.Abstractions;
using ARCANet.Invoices;

namespace ARCANet.Tests.Invoices;

public sealed class InvoiceRequestValidatorTests
{
    private readonly InvoiceRequestValidator _validator = new(new FakeClock(new DateTimeOffset(2026, 5, 14, 12, 0, 0, TimeSpan.Zero)));

    [Fact]
    public void Validate_AcceptsValidMinimalFacturaBRequest()
    {
        var request = TestInvoiceFactory.CreateValidFacturaBRequest();

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_AcceptsValidFacturaARequestWithVatBreakdown()
    {
        var request = TestInvoiceFactory.CreateValidFacturaARequest();

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_RejectsInconsistentTotalAmount()
    {
        var request = TestInvoiceFactory.CreateValidFacturaBRequest() with
        {
            Totals = new MoneyTotals
            {
                TotalAmount = 1001.00m,
                TaxableAmount = 1000.00m
            }
        };

        var result = _validator.Validate(request);

        Assert.Contains(result.Errors, x => x.Code == "inconsistent_total_amount");
    }

    [Fact]
    public void Validate_RejectsInconsistentVatAmount()
    {
        var request = TestInvoiceFactory.CreateValidFacturaARequest() with
        {
            Totals = new MoneyTotals
            {
                TotalAmount = 1210.00m,
                TaxableAmount = 1000.00m,
                VatAmount = 200.00m
            }
        };

        var result = _validator.Validate(request);

        Assert.Contains(result.Errors, x => x.Code == "inconsistent_vat_amount");
    }

    [Fact]
    public void Validate_RejectsCreditNoteWithoutAssociatedVoucher()
    {
        var request = TestInvoiceFactory.CreateValidFacturaARequest() with
        {
            VoucherType = new VoucherType(3, "Nota de Credito A", VoucherKind.CreditNote),
            AssociatedVouchers = []
        };

        var result = _validator.Validate(request);

        Assert.Contains(result.Errors, x => x.Code == "associated_voucher_required");
    }

    [Fact]
    public void Validate_RejectsServiceInvoiceMissingServiceDates()
    {
        var request = TestInvoiceFactory.CreateValidFacturaBRequest() with
        {
            Concept = InvoiceConcept.Services,
            ServiceFrom = null,
            ServiceTo = null,
            PaymentDueDate = null
        };

        var result = _validator.Validate(request);

        Assert.Contains(result.Errors, x => x.Code == "service_from_required");
        Assert.Contains(result.Errors, x => x.Code == "service_to_required");
        Assert.Contains(result.Errors, x => x.Code == "payment_due_date_required");
    }

    [Fact]
    public void Validate_RejectsInvalidPointOfSale()
    {
        var request = TestInvoiceFactory.CreateValidFacturaBRequest() with
        {
            PointOfSale = 0
        };

        var result = _validator.Validate(request);

        Assert.Contains(result.Errors, x => x.Code == "invalid_point_of_sale");
    }

    [Fact]
    public void Validate_RejectsInvalidVoucherNumber()
    {
        var request = TestInvoiceFactory.CreateValidFacturaBRequest() with
        {
            VoucherNumber = 0
        };

        var result = _validator.Validate(request);

        Assert.Contains(result.Errors, x => x.Code == "invalid_voucher_number");
    }

    [Fact]
    public void Validate_RejectsInvalidCurrencyExchangeRate()
    {
        var request = TestInvoiceFactory.CreateValidFacturaBRequest() with
        {
            Currency = new CurrencyAmount("PES", 0)
        };

        var result = _validator.Validate(request);

        Assert.Contains(result.Errors, x => x.Code == "invalid_currency_exchange_rate");
    }

    [Fact]
    public void Validate_RejectsInvalidIssuerCuit()
    {
        var request = TestInvoiceFactory.CreateValidFacturaBRequest() with
        {
            IssuerCuit = 0
        };

        var result = _validator.Validate(request);

        Assert.Contains(result.Errors, x => x.Code == "invalid_issuer_cuit");
    }

    private sealed class FakeClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }
}
