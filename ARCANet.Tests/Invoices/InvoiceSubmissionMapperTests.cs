using ARCANet.InternalInvoices;

namespace ARCANet.Tests.Invoices;

public sealed class InvoiceSubmissionMapperTests
{
    [Fact]
    public void Map_ProducesExpectedNeutralInternalSubmission()
    {
        var mapper = new InvoiceSubmissionMapper();
        var request = TestInvoiceFactory.CreateValidFacturaARequest();

        var submission = mapper.Map(request);

        Assert.Equal(request.IssuerCuit, submission.IssuerCuit);
        Assert.Equal(request.IssuerCuit, submission.Series.IssuerCuit);
        Assert.Equal(request.PointOfSale, submission.Series.PointOfSale);
        Assert.Equal(request.VoucherType, submission.Series.VoucherType);
        Assert.Equal(request.VoucherNumber, submission.VoucherNumber);
        Assert.Equal(request.Concept, submission.Concept);
        Assert.Equal(request.IssueDate, submission.IssueDate);
        Assert.Equal(request.Customer.Name, submission.Receiver.Name);
        Assert.Equal(request.Customer.DocumentTypeCode, submission.Receiver.DocumentTypeCode);
        Assert.Equal(request.Customer.DocumentNumber, submission.Receiver.DocumentNumber);
        Assert.Equal(request.ReceiverVatCondition.Id, submission.Receiver.ReceiverVatConditionId);
        Assert.Equal(request.Currency, submission.Currency);
        Assert.Equal(request.Totals.TotalAmount, submission.Totals.TotalAmount);
        Assert.Single(submission.VatLines);
        Assert.Empty(submission.TributeLines);
        Assert.Empty(submission.AssociatedVouchers);
    }
}
