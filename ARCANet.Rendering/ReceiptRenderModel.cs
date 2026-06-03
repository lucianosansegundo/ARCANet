using ARCANet.Invoices;

namespace ARCANet.Rendering;

public sealed record ReceiptRenderModel
{
    public required AuthorizedInvoice Invoice { get; init; }
    public required IssuerDisplayInfo Issuer { get; init; }
    public IReadOnlyList<ReceiptLineItem> Items { get; init; } = [];
    public string? FooterText { get; init; }
    public string? PaymentDescription { get; init; }
    public string? CashierName { get; init; }
    public string? LogoDataUrl { get; init; }
}
