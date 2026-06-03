using ARCANet.Rendering;
using ARCANet.Rendering.Pdf;
using ARCANet.Tests.Invoices;

namespace ARCANet.Tests.Rendering.Pdf;

public sealed class ReceiptPdfRendererTests
{
    [Fact]
    public void RenderPdf_A4_GeneratesPdfAndSupportsStreamOutput()
    {
        var renderer = new ReceiptPdfRenderer();
        var model = CreateModel();
        var pdf = renderer.RenderPdf(
            model,
            new ReceiptPdfRenderOptions
            {
                Layout = ReceiptPdfPageLayout.A4
            });

        using var stream = new MemoryStream();
        renderer.RenderPdf(
            model,
            stream,
            new ReceiptPdfRenderOptions
            {
                Layout = ReceiptPdfPageLayout.A4
            });

        Assert.StartsWith("%PDF-", ConvertPdfPrefix(pdf), StringComparison.Ordinal);
        Assert.True(pdf.Length > 2_000);
        Assert.StartsWith("%PDF-", ConvertPdfPrefix(stream.ToArray()), StringComparison.Ordinal);
        Assert.True(stream.Length > 2_000);
        Assert.Contains("/Image", System.Text.Encoding.Latin1.GetString(pdf), StringComparison.Ordinal);
    }

    [Fact]
    public void RenderPdf_Thermal58Mm_GeneratesDistinctPdfLayout()
    {
        var renderer = new ReceiptPdfRenderer();
        var model = CreateModel();
        var a4Pdf = renderer.RenderPdf(
            model,
            new ReceiptPdfRenderOptions
            {
                Layout = ReceiptPdfPageLayout.A4
            });
        var thermalPdf = renderer.RenderPdf(
            model,
            new ReceiptPdfRenderOptions
            {
                Layout = ReceiptPdfPageLayout.Thermal58Mm
            });

        Assert.StartsWith("%PDF-", ConvertPdfPrefix(thermalPdf), StringComparison.Ordinal);
        Assert.True(thermalPdf.Length > 1_500);
        Assert.NotEqual(Convert.ToBase64String(a4Pdf), Convert.ToBase64String(thermalPdf));
        Assert.Contains("/Image", System.Text.Encoding.Latin1.GetString(thermalPdf), StringComparison.Ordinal);
    }

    [Fact]
    public void RenderPdf_Thermal80Mm_GeneratesDistinctPdfFrom58Mm()
    {
        var renderer = new ReceiptPdfRenderer();
        var model = CreateModel();
        var thermal58Pdf = renderer.RenderPdf(
            model,
            new ReceiptPdfRenderOptions
            {
                Layout = ReceiptPdfPageLayout.Thermal58Mm
            });
        var thermal80Pdf = renderer.RenderPdf(
            model,
            new ReceiptPdfRenderOptions
            {
                Layout = ReceiptPdfPageLayout.Thermal80Mm
            });

        Assert.StartsWith("%PDF-", ConvertPdfPrefix(thermal80Pdf), StringComparison.Ordinal);
        Assert.True(thermal80Pdf.Length > 1_500);
        Assert.NotEqual(Convert.ToBase64String(thermal58Pdf), Convert.ToBase64String(thermal80Pdf));
        Assert.Contains("/Image", System.Text.Encoding.Latin1.GetString(thermal80Pdf), StringComparison.Ordinal);
    }

    private static ReceiptRenderModel CreateModel() =>
        new()
        {
            Invoice = TestInvoiceFactory.CreateAuthorizedFacturaA(),
            Issuer = new IssuerDisplayInfo
            {
                DisplayName = "Comercio Demo S.A.",
                TaxId = "30-71234567-8",
                VatConditionLabel = "IVA Responsable Inscripto",
                Address = "Av. Siempre Viva 123"
            },
            Items =
            [
                new ReceiptLineItem
                {
                    Description = "Aceite 2L",
                    Quantity = 2m,
                    UnitPrice = 500m,
                    DiscountAmount = 0m,
                    Subtotal = 1000m
                }
            ],
            PaymentDescription = "Debito",
            CashierName = "Caja 2",
            FooterText = "Conserve este comprobante."
        };

    private static string ConvertPdfPrefix(byte[] pdfBytes) =>
        System.Text.Encoding.ASCII.GetString(pdfBytes, 0, Math.Min(pdfBytes.Length, 8));
}
