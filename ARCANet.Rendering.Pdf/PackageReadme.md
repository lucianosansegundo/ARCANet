# ARCA.Fiscal.Rendering.Pdf

Modulo opcional de PDF para `ARCA.Fiscal`.

Este paquete ofrece:

- generacion de `PDF` para comprobantes fiscales autorizados
- salida `A4`
- salida termica continua para `80 mm`
- salida termica continua para `58 mm`
- reutilizacion del `ReceiptRenderModel` de `ARCA.Fiscal.Rendering`

Uso recomendado:

- `A4` como salida principal cuando la aplicacion necesita descarga, archivo, mail o reimpresion consistente
- para impresion termica pura, preferir `HTML` desde `ARCA.Fiscal.Rendering` y dejar este modulo PDF como salida opcional o documental

Uso basico:

```csharp
using ARCANet.Rendering;
using ARCANet.Rendering.Pdf;

var renderer = new ReceiptPdfRenderer();

var pdf = renderer.RenderPdf(
    model,
    new ReceiptPdfRenderOptions
    {
        Layout = ReceiptPdfPageLayout.A4
    });
```

Para salida termica:

```csharp
var thermalPdf = renderer.RenderPdf(
    model,
    new ReceiptPdfRenderOptions
    {
        Layout = ReceiptPdfPageLayout.Thermal80Mm
    });
```

Notas importantes:

- este modulo no emite comprobantes
- no descarga PDFs desde ARCA
- genera la representacion PDF a partir de datos fiscales ya autorizados y datos visuales extra
- usa `PDFsharp-MigraDoc` con licencia `MIT`
- incluye fuentes libres embebidas para no depender de Windows ni de fuentes instaladas en el host
