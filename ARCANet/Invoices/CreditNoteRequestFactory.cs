namespace ARCANet.Invoices;

public static class CreditNoteRequestFactory
{
    public static CreateInvoiceRequest CreateFullCancellation(
        AuthorizedInvoice originalInvoice,
        long voucherNumber,
        DateOnly issueDate,
        string? externalIdempotencyKey = null) =>
        CreateFromOriginal(
            originalInvoice,
            voucherNumber,
            issueDate,
            originalInvoice.Totals,
            originalInvoice.VatItems,
            originalInvoice.Tributes,
            externalIdempotencyKey);

    public static CreateInvoiceRequest CreatePartial(
        AuthorizedInvoice originalInvoice,
        long voucherNumber,
        DateOnly issueDate,
        MoneyTotals totals,
        IReadOnlyList<VatItem>? vatItems = null,
        IReadOnlyList<TributeItem>? tributes = null,
        string? externalIdempotencyKey = null)
    {
        ArgumentNullException.ThrowIfNull(totals);

        var partialVatItems = vatItems ?? [];
        var partialTributes = tributes ?? [];

        ValidatePartialAgainstOriginal(originalInvoice, totals, partialVatItems, partialTributes);

        return CreateFromOriginal(
            originalInvoice,
            voucherNumber,
            issueDate,
            totals,
            partialVatItems,
            partialTributes,
            externalIdempotencyKey);
    }

    private static CreateInvoiceRequest CreateFromOriginal(
        AuthorizedInvoice originalInvoice,
        long voucherNumber,
        DateOnly issueDate,
        MoneyTotals totals,
        IReadOnlyList<VatItem> vatItems,
        IReadOnlyList<TributeItem> tributes,
        string? externalIdempotencyKey)
    {
        ArgumentNullException.ThrowIfNull(originalInvoice);

        if (voucherNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(voucherNumber), "Voucher number must be greater than zero.");
        }

        if (issueDate == default)
        {
            throw new ArgumentException("Issue date is required.", nameof(issueDate));
        }

        return new CreateInvoiceRequest
        {
            IssuerCuit = originalInvoice.IssuerCuit,
            VoucherType = MapCreditNoteVoucherType(originalInvoice.Series.VoucherType),
            PointOfSale = originalInvoice.Series.PointOfSale,
            VoucherNumber = voucherNumber,
            Concept = originalInvoice.Concept,
            IssueDate = issueDate,
            ServiceFrom = originalInvoice.ServiceFrom,
            ServiceTo = originalInvoice.ServiceTo,
            PaymentDueDate = originalInvoice.PaymentDueDate,
            Customer = originalInvoice.Customer,
            ReceiverVatCondition = originalInvoice.ReceiverVatCondition,
            Totals = totals,
            Currency = originalInvoice.Currency,
            VatItems = vatItems,
            Tributes = tributes,
            AssociatedVouchers =
            [
                new AssociatedVoucher
                {
                    VoucherType = originalInvoice.Series.VoucherType,
                    PointOfSale = originalInvoice.Series.PointOfSale,
                    VoucherNumber = originalInvoice.VoucherNumber,
                    IssuerCuit = originalInvoice.IssuerCuit,
                    IssuedOn = originalInvoice.IssueDate
                }
            ],
            ExternalIdempotencyKey = externalIdempotencyKey
        };
    }

    private static VoucherType MapCreditNoteVoucherType(VoucherType originalVoucherType) =>
        originalVoucherType.Code switch
        {
            1 => new VoucherType(3, "Nota de Credito A", VoucherKind.CreditNote),
            6 => new VoucherType(8, "Nota de Credito B", VoucherKind.CreditNote),
            _ => throw new InvalidOperationException(
                $"Automatic credit note creation is currently only supported for Factura A (1) and Factura B (6). Original voucher type was '{originalVoucherType.Code} - {originalVoucherType.Name}'.")
        };

    private static void ValidatePartialAgainstOriginal(
        AuthorizedInvoice originalInvoice,
        MoneyTotals totals,
        IReadOnlyList<VatItem> vatItems,
        IReadOnlyList<TributeItem> tributes)
    {
        ArgumentNullException.ThrowIfNull(originalInvoice);

        if (totals.TotalAmount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totals), "Partial credit note total amount must be greater than zero.");
        }

        ValidateNotExceed(totals.TotalAmount, originalInvoice.Totals.TotalAmount, nameof(MoneyTotals.TotalAmount));
        ValidateNotExceed(totals.TaxableAmount, originalInvoice.Totals.TaxableAmount, nameof(MoneyTotals.TaxableAmount));
        ValidateNotExceed(totals.NonTaxedAmount, originalInvoice.Totals.NonTaxedAmount, nameof(MoneyTotals.NonTaxedAmount));
        ValidateNotExceed(totals.ExemptAmount, originalInvoice.Totals.ExemptAmount, nameof(MoneyTotals.ExemptAmount));
        ValidateNotExceed(totals.VatAmount, originalInvoice.Totals.VatAmount, nameof(MoneyTotals.VatAmount));
        ValidateNotExceed(totals.OtherTaxesAmount, originalInvoice.Totals.OtherTaxesAmount, nameof(MoneyTotals.OtherTaxesAmount));

        var expectedVatAmount = vatItems.Sum(x => x.Amount);
        if (expectedVatAmount != totals.VatAmount)
        {
            throw new InvalidOperationException("Partial credit note VAT amount must equal the sum of VAT item amounts.");
        }

        var expectedTributeAmount = tributes.Sum(x => x.Amount);
        if (expectedTributeAmount != totals.OtherTaxesAmount)
        {
            throw new InvalidOperationException("Partial credit note other taxes amount must equal the sum of tribute item amounts.");
        }
    }

    private static void ValidateNotExceed(decimal value, decimal originalValue, string fieldName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(fieldName, $"{fieldName} cannot be negative.");
        }

        if (value > originalValue)
        {
            throw new InvalidOperationException($"Partial credit note {fieldName} cannot exceed the original invoice value.");
        }
    }
}
