# Numeracion y Recuperacion para POS

Guia operativa para integrar `ARCANet` en un `POS` real sin delegarle al SDK problemas que siguen siendo propios de la aplicacion:

- numeracion
- idempotencia funcional
- estados inciertos
- retry seguro

Importante:

- esta guia no reemplaza validacion contable/fiscal profesional
- esta guia asume emision por `WSFEv1`
- esta guia no convierte a `ARCANet` en el duenio de la numeracion global

## 1. Que resuelve ARCANet y que no

`ARCANet` ya resuelve:

- WSAA
- emision y consulta WSFEv1
- `UnknownInvoiceResult` para fallos tecnicos inciertos
- `InvoiceSubmissionRecovery` para consulta posterior con `FECompConsultar`

`ARCANet` no resuelve por vos:

- reserva global de numero
- lock distribuido entre cajas o nodos
- persistencia transaccional del intento
- decision de retry automatico
- politica de deduplicacion de ventas de negocio

Ese boundary es intencional.

## 2. Regla base

No hagas retry ciego de `CreateInvoiceAsync`.

Si el resultado es:

- `AuthorizedInvoiceResult`: persisti y terminaste
- `RejectedInvoiceResult`: persisti rechazo y no reintentes automaticamente
- `UnknownInvoiceResult`: primero consulta, despues decidi

## 3. Modelo minimo que deberia persistir tu POS

Antes de invocar ARCA, la app deberia guardar un intento local con al menos:

- `IssuerCuit`
- `PointOfSale`
- `VoucherType`
- `VoucherNumber`
- `IssueDate`
- `ReceiverDocumentType`
- `ReceiverDocumentNumber`
- `TotalAmount`
- `ExternalIdempotencyKey`
- `Status`
- `CreatedAtUtc`
- `UpdatedAtUtc`

Campos utiles adicionales:

- `BusinessSaleId`
- `CashRegisterId`
- `OperatorId`
- `AttemptCount`
- `LastError`
- `AuthorizationCode`
- `AuthorizationDueDate`

## 4. Estados recomendados

Estados sugeridos para la app consumidora:

- `Draft`
- `NumberReserved`
- `SubmissionInProgress`
- `Authorized`
- `Rejected`
- `UnknownNeedsVerification`
- `CancelledInternally`

Interpretacion:

- `Draft`: la venta existe, pero todavia no hay numero reservado
- `NumberReserved`: ya reservaste `PtoVta + Tipo + Numero`
- `SubmissionInProgress`: el request esta saliendo a ARCA
- `Authorized`: ARCA autorizo
- `Rejected`: ARCA rechazo funcionalmente
- `UnknownNeedsVerification`: fallo tecnico incierto; no se debe reintentar sin consulta

## 5. Numeracion

La numeracion debe coordinarse por:

- `IssuerCuit`
- `PointOfSale`
- `VoucherType`

La regla operativa es:

1. abrir transaccion local
2. tomar lock exclusivo para esa serie
3. calcular o reservar el siguiente numero
4. persistir el intento en `NumberReserved` o `SubmissionInProgress`
5. confirmar la transaccion local
6. recien ahi llamar a `CreateInvoiceAsync`

No uses `GetLastAuthorizedNumberAsync` como mecanismo primario de numeracion concurrente.

Sirve para:

- bootstrap
- diagnostico
- conciliacion

No sirve por si solo para evitar colisiones entre dos cajas o dos procesos.

## 6. Idempotencia funcional

Tu POS deberia tener una clave funcional propia por venta o evento de negocio.

Ejemplos razonables:

- `sale:{saleId}:invoice-b`
- `refund:{refundId}:credit-note-b`
- `order:{orderId}:invoice-a`

La idea no es que la clave codifique todo el request.

La idea es que identifique:

- el hecho de negocio
- el tipo de comprobante esperado

Recomendacion:

- guarda esa clave en tu propia base con indice unico
- pasa esa misma clave como `ExternalIdempotencyKey` a `CreateInvoiceRequest`

`ARCANet` hoy no usa esa clave para deduplicar automaticamente contra ARCA.
Sirve para:

- trazabilidad
- correlacion
- auditoria
- politica de deduplicacion en tu propia app

## 7. Flujo recomendado de emision

Flujo pragmatico:

1. crear o recuperar la venta local
2. abrir transaccion
3. verificar si la clave funcional ya tiene un intento finalizado
4. si ya esta `Authorized`, devolver ese resultado local
5. si ya esta `Rejected`, devolver ese resultado local
6. si no existe, reservar numero y persistir intento
7. cerrar transaccion
8. llamar a `CreateInvoiceAsync`
9. persistir el resultado

Persistencia sugerida por tipo de resultado:

- `AuthorizedInvoiceResult`:
  - estado `Authorized`
  - CAE
  - vencimiento
  - QR
  - snapshot del request
- `RejectedInvoiceResult`:
  - estado `Rejected`
  - rechazos/observaciones
  - snapshot del request
- `UnknownInvoiceResult`:
  - estado `UnknownNeedsVerification`
  - motivo tecnico
  - snapshot del request

## 8. Que hacer ante UnknownInvoiceResult

Si obtienes `UnknownInvoiceResult`, el siguiente paso conservador es:

```csharp
using ARCANet.Invoices;

if (result is UnknownInvoiceResult unknown)
{
    var recovery = new InvoiceSubmissionRecovery(invoiceClient);
    var reconciliation = await recovery.ReconcileAsync(unknown, cancellationToken);

    if (reconciliation is AuthorizedInvoiceReconciliationResult authorized)
    {
        // persistir Authorized
    }
    else if (reconciliation is UnconfirmedInvoiceReconciliationResult)
    {
        // sigue incierto a nivel tecnico/operativo
    }
}
```

Interpretacion:

- `AuthorizedInvoiceReconciliationResult`: el comprobante existe en ARCA; no reintentes la emision
- `UnconfirmedInvoiceReconciliationResult`: la consulta no lo confirmo; todavia no significa que sea seguro reintentar

## 9. Politica de retry recomendada

Regla conservadora:

- no hacer retry automatico cuando ya hubo `UnknownInvoiceResult`

Politica sugerida:

- error claramente previo al envio:
  - retry posible
- `UnknownInvoiceResult`:
  - consultar primero
  - si sigue sin confirmarse, escalar a un flujo manual o a una politica interna bien definida
- `RejectedInvoiceResult`:
  - no retry automatico

Ejemplos de error claramente previo al envio:

- validacion local fallida
- cancelacion local antes de hacer IO real
- dependencia interna caida antes de invocar transporte

Ejemplos de error incierto:

- timeout HTTP al esperar respuesta
- corte de red luego de enviar
- fallo SOAP/parseo que no permite concluir si ARCA proceso o no

## 10. Reconciliacion posterior

Conviene tener un proceso de reconciliacion diferida para intentos en:

- `UnknownNeedsVerification`

Ese proceso puede:

1. tomar lotes de intentos inciertos
2. consultar `FECompConsultar`
3. cerrar los que aparezcan como autorizados
4. dejar evidencia de los que sigan sin confirmarse
5. escalar revision manual o una politica interna de reemision

Esto es especialmente importante si:

- hay multiples cajas
- hay multiples instancias del backend
- hay reintentos por usuario
- hay integracion asincronica con otros sistemas

## 11. Lo que no conviene hacer

No conviene:

- usar `GetLastAuthorizedNumberAsync + 1` como unica estrategia productiva de numeracion concurrente
- reintentar `CreateInvoiceAsync` automaticamente apenas hay timeout
- asumir que `Unconfirmed` equivale a "seguro no emitido"
- depender solo de `FECompConsultar` para reconstruir todos los datos visuales del comprobante
- no persistir tu propio snapshot del request autorizado o rechazado

## 12. Recomendacion minima para una app real

Si queres un baseline razonable para salir del modo demo:

- usa una tabla local de intentos de factura
- pone un indice unico sobre tu clave funcional
- coordina numeracion por `PtoVta + Tipo`
- guarda snapshot del request y del resultado
- usa `PostgresAccessTicketStore` si corres multi-instancia
- usa `InvoiceSubmissionRecovery` para estados inciertos

## 13. Relacion con el roadmap

Esta guia cubre el boundary operativo recomendado hoy para:

- `Factura A`
- `Factura B`
- `Nota de Credito A`
- `Nota de Credito B`

No cubre todavia:

- `CAEA`
- contingencia offline
- sincronizacion compleja entre sucursales desconectadas
- hardware fiscal
- regimenes especiales
