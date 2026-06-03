using ARCANet.Rendering;
using ARCANet.Tests.Invoices;

namespace ARCANet.Tests.Rendering;

public sealed class ThermalReceiptHtmlRendererTests
{
    [Fact]
    public void RenderHtml_UsesThermalWidthAndCompactSections()
    {
        var renderer = new ThermalReceiptHtmlRenderer();
        var model = new ReceiptRenderModel
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
                    Subtotal = 1000m
                }
            ],
            PaymentDescription = "Debito",
            CashierName = "Caja 2",
            FooterText = "Conserve este ticket."
        };

        var html = renderer.RenderHtml(model);

        Assert.Contains("@page", html);
        Assert.Contains("80mm", html);
        Assert.Contains("DETALLE", html);
        Assert.Contains("TOTALES", html);
        Assert.Contains("Aceite 2L", html);
        Assert.Contains("Caja 2", html);
        Assert.Contains("Conserve este ticket.", html);
        Assert.Contains("<svg", html);
        Assert.Contains("Escanee el QR para validar.", html);
        Assert.DoesNotContain("https://www.afip.gob.ar/fe/qr/", html);
    }

    [Fact]
    public void RenderHtml_OmitsOptionalCommercialSectionsWhenMissing()
    {
        var renderer = new ThermalReceiptHtmlRenderer();
        var model = new ReceiptRenderModel
        {
            Invoice = TestInvoiceFactory.CreateAuthorizedCreditNoteB(),
            Issuer = new IssuerDisplayInfo
            {
                DisplayName = "Comercio Demo S.A.",
                TaxId = "30-71234567-8",
                VatConditionLabel = "IVA Responsable Inscripto"
            }
        };

        var html = renderer.RenderHtml(model);

        Assert.Contains("ASOCIADOS", html);
        Assert.DoesNotContain("DETALLE", html);
        Assert.DoesNotContain("Caja", html);
        Assert.DoesNotContain("Pago", html);
    }
}
