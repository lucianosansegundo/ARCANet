using ARCANet.Invoices;

namespace ARCANet.InternalInvoices;

internal sealed class InvoiceSubmissionMapper : IInvoiceSubmissionMapper
{
    public InternalInvoiceSubmission Map(CreateInvoiceRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new InternalInvoiceSubmission
        {
            IssuerCuit = request.IssuerCuit,
            Series = new VoucherSeries(request.IssuerCuit, request.PointOfSale, request.VoucherType),
            VoucherNumber = request.VoucherNumber,
            Concept = request.Concept,
            IssueDate = request.IssueDate,
            ServiceFrom = request.ServiceFrom,
            ServiceTo = request.ServiceTo,
            PaymentDueDate = request.PaymentDueDate,
            Receiver = new InternalInvoiceReceiver(
                request.Customer.Name,
                request.Customer.IsConsumerFinal,
                request.Customer.DocumentTypeCode,
                request.Customer.DocumentNumber,
                request.ReceiverVatCondition.Id,
                request.ReceiverVatCondition.Name),
            Currency = request.Currency,
            Totals = new InternalInvoiceTotals(
                request.Totals.TotalAmount,
                request.Totals.TaxableAmount,
                request.Totals.NonTaxedAmount,
                request.Totals.ExemptAmount,
                request.Totals.VatAmount,
                request.Totals.OtherTaxesAmount),
            VatLines = request.VatItems
                .Select(x => new InternalVatLine(x.Id, x.BaseAmount, x.Rate, x.Amount))
                .ToArray(),
            TributeLines = request.Tributes
                .Select(x => new InternalTributeLine(x.Id, x.Description, x.BaseAmount, x.Rate, x.Amount))
                .ToArray(),
            AssociatedVouchers = request.AssociatedVouchers
                .Select(x => new InternalAssociatedVoucher(
                    x.VoucherType,
                    x.PointOfSale,
                    x.VoucherNumber,
                    x.IssuerCuit,
                    x.IssuedOn))
                .ToArray(),
            ExternalIdempotencyKey = request.ExternalIdempotencyKey
        };
    }
}
