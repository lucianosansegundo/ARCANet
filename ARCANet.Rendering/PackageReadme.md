# ARCA.Fiscal.Rendering

Modulo opcional de rendering generico para `ARCA.Fiscal`.

Este paquete ofrece:

- rendering `HTML` imprimible para comprobantes ya autorizados
- una variante generica de pagina con `HtmlReceiptRenderer`
- una variante compacta para rollo termico de `58/80 mm` con `ThermalReceiptHtmlRenderer`
- un modelo separado del core fiscal para sumar datos visibles del emisor
- detalle comercial opcional de items para escenarios POS
- bloque fiscal con importes, CAE o CAEA, comprobantes asociados y QR

Uso recomendado:

- `HTML` generico para preview o impresion simple
- `HTML` termico como salida principal para impresion en `58/80 mm`
- si la aplicacion necesita `A4` documental, usar el modulo separado `ARCA.Fiscal.Rendering.Pdf`

Uso basico:

```csharp
using ARCANet.Rendering;

var model = new ReceiptRenderModel
{
    Invoice = authorizedInvoice,
    Issuer = new IssuerDisplayInfo
    {
        DisplayName = "Comercio Demo S.A.",
        TaxId = "30-71234567-8",
        VatConditionLabel = "IVA Responsable Inscripto",
        Address = "Av. Demo 123, CABA"
    },
    Items =
    [
        new ReceiptLineItem
        {
            Description = "Producto A",
            Quantity = 2,
            UnitPrice = 5000m,
            Subtotal = 10000m
        }
    ],
    PaymentDescription = "Tarjeta de debito",
    CashierName = "Caja 1"
};

var renderer = new HtmlReceiptRenderer();
var html = renderer.RenderHtml(model);
```

Para ticket termico:

```csharp
var renderer = new ThermalReceiptHtmlRenderer();
var html = renderer.RenderHtml(model);
```

Boundary importante:

- este modulo no emite comprobantes
- no numera
- no persiste
- no reemplaza templates propios del POS si la app necesita branding o layouts custom
- la salida PDF vive en el modulo separado `ARCA.Fiscal.Rendering.Pdf`
- la salida termica cubre `58 mm` y `80 mm`, pero todavia requiere validacion real con navegador, driver e impresora concretos
