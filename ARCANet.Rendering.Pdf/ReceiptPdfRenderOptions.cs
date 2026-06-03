namespace ARCANet.Rendering.Pdf;

public sealed record ReceiptPdfRenderOptions
{
    public ReceiptPdfPageLayout Layout { get; init; } = ReceiptPdfPageLayout.A4;
}
