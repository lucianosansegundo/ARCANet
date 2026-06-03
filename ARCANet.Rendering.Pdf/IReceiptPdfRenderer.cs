namespace ARCANet.Rendering.Pdf;

public interface IReceiptPdfRenderer
{
    byte[] RenderPdf(ReceiptRenderModel model, ReceiptPdfRenderOptions? options = null);
    void RenderPdf(ReceiptRenderModel model, Stream output, ReceiptPdfRenderOptions? options = null);
}
