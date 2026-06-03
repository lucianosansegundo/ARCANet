namespace ARCANet.Rendering;

public sealed record ReceiptLineItem
{
    public required string Description { get; init; }
    public decimal Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal Subtotal { get; init; }
}
