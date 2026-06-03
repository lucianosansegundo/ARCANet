# Rendering generico

`ARCANet.Rendering` es el primer modulo opcional de rendering para comprobantes autorizados.

Ahora tambien existe `ARCANet.Rendering.Pdf` como modulo separado para salida PDF.

Objetivo de esta etapa:

- mantener el rendering fuera del core fiscal
- ofrecer una salida `HTML` generica e imprimible
- permitir sumar al comprobante fiscal datos visibles del emisor y detalle comercial del POS

Incluye en esta version:

- `HtmlReceiptRenderer`
- `ThermalReceiptHtmlRenderer`
- `ReceiptRenderModel`
- `IssuerDisplayInfo`
- `ReceiptLineItem`
- `ReceiptPdfRenderer` en el modulo `ARCANet.Rendering.Pdf`

Boundary con el POS:

- `ARCANet` sigue aportando el comprobante autorizado y sus datos fiscales
- el POS sigue aportando razon social visible, domicilio comercial, condicion IVA mostrable si la necesita, logo, items, medio de pago y datos operativos
- esta primera version no busca reemplazar templates propios ni branding custom

Formato recomendado segun uso:

- `A4`: `PDF` como salida principal
- `thermal58` y `thermal80`: `HTML` como salida principal para impresion
- el `PDF` termico sigue disponible como salida documental o de validacion, pero no reemplaza una integracion de impresion real del POS

Limitaciones actuales:

- `ARCANet.Rendering` solo genera `HTML`
- el `PDF` vive en un modulo separado
- el modulo PDF apunta deliberadamente a una salida fiscal basica
- el modulo PDF embebe sus propias fuentes para ser portable entre Windows, Linux y contenedores
- no tiene themes ni personalizacion avanzada
- esta pensada como base generica para `Factura A/B` y `Nota de Credito A/B`
- las variantes termicas apuntan a `58 mm` y `80 mm`, pero todavia deben validarse en hardware real antes de considerarlas listas para produccion
