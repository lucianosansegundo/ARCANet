using ARCANet.Invoices;

namespace ARCANet.Abstractions;

public interface IInvoiceRequestValidator
{
    InvoiceValidationResult Validate(CreateInvoiceRequest request);
}
