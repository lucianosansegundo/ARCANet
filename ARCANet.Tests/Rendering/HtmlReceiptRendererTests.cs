using ARCANet.Invoices;
using ARCANet.Rendering;
using ARCANet.Tests.Invoices;

namespace ARCANet.Tests.Rendering;

public sealed class HtmlReceiptRendererTests
{
    [Fact]
    public void RenderHtml_IncludesFiscalAndCommercialData()
    {
        var renderer = new HtmlReceiptRenderer();
        var invoice = TestInvoiceFactory.CreateAuthorizedFacturaA();
        var model = new ReceiptRenderModel
        {
            Invoice = invoice,
            Issuer = new IssuerDisplayInfo
            {
                DisplayName = "Comercio Demo S.A.",
                TaxId = "30-71234567-8",
                VatConditionLabel = "IVA Responsable Inscripto",
                Address = "Av. Siempre Viva 123",
                GrossIncomeNumber = "902-123456-7",
                BusinessStartDate = new DateOnly(2020, 1, 15)
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
            PaymentDescription = "Transferencia",
            CashierName = "Caja 1",
            FooterText = "Gracias por su compra."
        };

        var html = renderer.RenderHtml(model);

        Assert.Contains("Factura A", html);
        Assert.Contains("00005-00001234", html);
        Assert.Contains("Cliente SA", html);
        Assert.Contains("IVA Responsable Inscripto", html);
        Assert.Contains("Aceite 2L", html);
        Assert.Contains("Transferencia", html);
        Assert.Contains("Caja 1", html);
        Assert.Contains("CAE", html);
        Assert.Contains("12345678901234", html);
        Assert.Contains("01/06/2026", html);
        Assert.Contains("QR fiscal", html);
        Assert.Contains("<svg", html);
        Assert.Contains("Gracias por su compra.", html);
    }

    [Fact]
    public void RenderHtml_RendersAssociatedVouchersWithoutCommercialSections()
    {
        var renderer = new HtmlReceiptRenderer();
        var invoice = TestInvoiceFactory.CreateAuthorizedCreditNoteB();
        var model = new ReceiptRenderModel
        {
            Invoice = invoice,
            Issuer = new IssuerDisplayInfo
            {
                DisplayName = "Comercio Demo S.A.",
                TaxId = "30-71234567-8",
                VatConditionLabel = "IVA Responsable Inscripto"
            }
        };

        var html = renderer.RenderHtml(model);

        Assert.Contains("Nota de Credito B", html);
        Assert.Contains("Comprobantes asociados", html);
        Assert.Contains("Factura B 00005-00004321", html);
        Assert.Contains("Condicion IVA:</strong> Consumidor Final", html);
        Assert.Contains("Documento:</strong> No informado", html);
        Assert.DoesNotContain("Detalle comercial", html);
        Assert.DoesNotContain("Datos operativos", html);
    }
}
