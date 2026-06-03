namespace ARCANet.Rendering;

public sealed record IssuerDisplayInfo
{
    public required string DisplayName { get; init; }
    public required string TaxId { get; init; }
    public required string VatConditionLabel { get; init; }
    public string? Address { get; init; }
    public string? GrossIncomeNumber { get; init; }
    public DateOnly? BusinessStartDate { get; init; }
}
