namespace ARCANet.Invoices;

public sealed record InvoiceValidationError(
    string Code,
    string Message,
    string Field,
    InvoiceValidationSeverity Severity = InvoiceValidationSeverity.Error);
