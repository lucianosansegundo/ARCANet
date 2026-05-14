namespace ARCANet.Invoices;

public sealed record InvoiceValidationResult
{
    public static InvoiceValidationResult Success { get; } = new();

    public IReadOnlyList<InvoiceValidationError> Errors { get; init; } = [];

    public bool IsValid => Errors.Count == 0;
}
