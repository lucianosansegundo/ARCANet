using ARCANet.Invoices;

namespace ARCANet.InternalInvoices;

internal interface IInvoiceSubmissionMapper
{
    InternalInvoiceSubmission Map(CreateInvoiceRequest request);
}
