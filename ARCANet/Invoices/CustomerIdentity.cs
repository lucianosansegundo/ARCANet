namespace ARCANet.Invoices;

public sealed record CustomerIdentity
{
    public required string Name { get; init; }
    public int? DocumentTypeCode { get; init; }
    public string? DocumentNumber { get; init; }
    public bool IsConsumerFinal { get; init; }
}
