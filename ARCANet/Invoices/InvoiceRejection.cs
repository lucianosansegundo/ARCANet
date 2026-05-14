namespace ARCANet.Invoices;

public sealed record InvoiceRejection(
    string Code,
    string Message);
