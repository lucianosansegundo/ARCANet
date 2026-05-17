# Uso de Notas de Credito

Guia practica para usar notas de credito en `ARCANet` dentro del alcance actual.

Importante:

- en este alcance, "anular" una operacion significa emitir una nota de credito asociada
- esta guia no reemplaza validacion contable/fiscal profesional
- esta guia cubre `Factura A/B -> Nota de Credito A/B`

## 1. Que resuelve el SDK hoy

`ARCANet` ya soporta:

- `Nota de Credito A`
- `Nota de Credito B`
- comprobantes asociados
- homologacion real de ambos casos
- construccion asistida desde una factura autorizada original con `CreditNoteRequestFactory`

Eso no significa que el SDK decida por vos:

- si corresponde una anulacion total o parcial
- que evento de negocio dispara la nota
- como numeras internamente
- como persistis el flujo operativo

## 2. Cuando pensar en nota de credito

En terminos de negocio, suele aparecer para:

- anulacion total de una venta
- devolucion parcial
- correccion de importes
- bonificacion posterior

Desde el punto de vista tecnico/fiscal dentro de este SDK, la idea clave es:

- partir del comprobante original autorizado
- asociar la nota a ese comprobante
- usar importes consistentes con el ajuste que realmente queres reflejar

## 3. Cancelacion total

Si queres anular completamente una `Factura A` o `Factura B`, el camino recomendado es derivar la nota directamente desde la factura autorizada original:

```csharp
using ARCANet.Invoices;

var request = CreditNoteRequestFactory.CreateFullCancellation(
    originalInvoice,
    voucherNumber: nextNumber,
    issueDate: today,
    externalIdempotencyKey: $"refund:{refundId}:full");
```

Que preserva automaticamente:

- receptor
- condicion IVA del receptor
- concepto
- moneda
- importes
- IVA
- tributos
- comprobante asociado

Que tenes que seguir resolviendo en tu app:

- `nextNumber`
- `issueDate`
- persistencia del intento
- politicas de idempotencia

## 4. Ajuste parcial

Si no queres anular todo, sino solo una parte, tenes que informar explicitamente los importes parciales.

Ejemplo:

```csharp
var request = CreditNoteRequestFactory.CreatePartial(
    originalInvoice,
    voucherNumber: nextNumber,
    issueDate: today,
    totals: new MoneyTotals
    {
        TotalAmount = 605.00m,
        TaxableAmount = 500.00m,
        VatAmount = 105.00m
    },
    vatItems:
    [
        new VatItem
        {
            Id = 5,
            BaseAmount = 500.00m,
            Rate = 21.00m,
            Amount = 105.00m
        }
    ],
    externalIdempotencyKey: $"refund:{refundId}:partial");
```

Reglas que el helper valida:

- el parcial no puede exceder a la factura original
- la suma de `VatItems` debe coincidir con `Totals.VatAmount`
- la suma de `Tributes` debe coincidir con `Totals.OtherTaxesAmount`

Lo que el helper no decide:

- cuanto devolver
- como dividir la devolucion entre items de negocio
- que alicuota corresponde si tu logica fiscal de aplicacion cambia el ajuste

## 5. Que tipo de nota crea

Mapeo actual:

- `Factura A (1) -> Nota de Credito A (3)`
- `Factura B (6) -> Nota de Credito B (8)`

Si el comprobante original no es `Factura A` ni `Factura B`, el helper hoy falla de forma explicita.

Eso es intencional para no inventar comportamiento fuera del alcance ya validado.

## 6. Que conviene persistir

Ademas del comprobante emitido, conviene persistir:

- referencia al comprobante original
- motivo de la nota de credito en tu dominio
- clave funcional de idempotencia
- snapshot del request enviado
- resultado autorizado/rechazado/incierto

Especialmente en anulaciones parciales, conviene guardar tambien:

- que lineas o importes de negocio se ajustaron
- quien autorizo la operacion
- trazabilidad con devolucion de stock o caja, si aplica

## 7. Flujo recomendado

Flujo pragmatico:

1. recuperar la factura original autorizada desde tu persistencia
2. decidir si la nota es total o parcial
3. reservar numero para la nota
4. construir `CreateInvoiceRequest` con `CreditNoteRequestFactory`
5. llamar a `CreateInvoiceAsync`
6. persistir resultado
7. si el resultado es incierto, usar `InvoiceSubmissionRecovery`

## 8. Que no conviene hacer

No conviene:

- reconstruir a mano siempre los `AssociatedVouchers`
- copiar importes parciales sin validar IVA
- emitir nota de credito sin persistir referencia al comprobante original
- tratar una nota parcial como si fuera automaticamente una anulacion total

## 9. Estado actual del alcance

Casos ya verificados en homologacion real:

- `Factura A`
- `Factura B`
- `Nota de Credito A`
- `Nota de Credito B`

La ergonomia actual ya es razonable para:

- cancelacion total
- devolucion parcial simple

Todavia no cubre de forma especializada:

- `Factura C`
- `Factura M`
- notas de debito
- helpers de negocio mas opinionados
