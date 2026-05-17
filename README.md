# ARCANet

Libreria open source .NET para ARCA/AFIP enfocada en una API mas segura y de mas alto nivel para aplicaciones reales.

Alcance inicial:

- autenticacion WSAA detras de escena
- facturacion WSFEv1 detras de una API orientada a dominio
- homologacion y pruebas primero
- `Factura A/B` y `Nota de Credito A/B`
- helpers para consulta de comprobantes y ultimo autorizado
- generacion de payload/URL/imagen del QR fiscal
- validacion local de requests, mapping interno neutral y modelado explicito de resultados

No objetivos del core inicial:

- exponer modelos SOAP/WSDL como API publica
- rendering de PDF, HTML o ticket termico
- templates visuales de comprobantes

Objetivos de diseno:

- ocultar `token`, `sign`, `FECAESolicitar` y clases SOAP al consumidor
- mantener el SDK generico y reusable
- separar preocupaciones del core fiscal y del rendering
- modelar explicitamente resultados autorizados, rechazados y estados inciertos

Estado actual:

- andamiaje de API publica
- validacion local de comprobantes
- mapping interno neutral del request de emision
- piezas core de WSAA implementadas sin proxies WSDL
- piezas core de WSFEv1 implementadas sin proxies WSDL
- `InvoiceClient` publico sobre validacion + WSAA + WSFEv1 + QR
- generacion de QR fiscal como payload/JSON/Base64/URL/SVG/PNG
- tests unitarios para validacion, mapping, QR, WSAA, WSFEv1 y orquestacion de invoices
- reconstruccion consultada desde `FECompConsultar` para campos fiscales que devuelve ARCA
- persistencia de access tickets WSAA con stores enchufables
- homologacion real validada para `Factura A/B` y `Nota de Credito A/B`
- todavia no listo para produccion

Importante:

- `ARCANet` no reemplaza validacion contable/fiscal profesional
- reglas regulatorias mutables y umbrales monetarios deben tratarse como configurables, no como verdades hardcodeadas
- `FECompConsultar` no devuelve todos los campos de negocio/presentacion; la app consumidora debe persistir sus propios snapshots
- el reuso de access tickets usa por defecto `IAccessTicketStore` en memoria; la app puede inyectar un store durable sin cambiar la API de invoices

Implementado actualmente:

- `CreateInvoiceRequest` y modelos de resultado
- `InvoiceRequestValidator`
- `CreditNoteRequestFactory`
- `InternalInvoiceSubmission` y mapping neutral interno
- `ArcaQrGenerator`
- `WsaaAccessTicketProvider`
- `IAccessTicketStore`, `InMemoryAccessTicketStore`, `NullAccessTicketStore`
- `FileAccessTicketStore`
- `Wsfev1Client`
- `InvoiceClient`
- `InvoiceSubmissionRecovery`
- transporte SOAP crudo via `HttpClientSoapTransport`

Modulos opcionales de persistencia:

- `ARCANet.Persistence.Postgres`
  - `PostgresAccessTicketStore`
  - `PostgresAccessTicketStoreOptions`
  - `PostgresAccessTicketStore.CreateInitializedAsync(...)`

## Persistencia de access tickets en PostgreSQL

```csharp
using ARCANet.Persistence.Postgres;
using ARCANet.Wsaa;

await using var ticketStore = await PostgresAccessTicketStore.CreateInitializedAsync(
    connectionString,
    new PostgresAccessTicketStoreOptions
    {
        SchemaName = "public",
        TableName = "arca_access_tickets"
    },
    cancellationToken);

var accessTicketProvider = new WsaaAccessTicketProvider(
    certificateProvider,
    transport,
    clock,
    wsaaOptions,
    ticketStore);
```

Ver [Persistencia de access tickets](docs/access-ticket-persistence.md) para criterios de seleccion de store y detalles de configuracion.

## Seleccion de ambiente

`ARCANet` distingue explicitamente entre ambientes ARCA:

- `ArcaEnvironment.Homologation`
- `ArcaEnvironment.Production`

El valor por defecto actual es `Homologation`.

Para un POS, el mapeo habitual es:

- modo test => `ArcaEnvironment.Homologation`
- modo real => `ArcaEnvironment.Production`

Ejemplo:

```csharp
using ARCANet.Configuration;
using ARCANet.Wsaa;
using ARCANet.Wsfev1;

var environment = isTestMode
    ? ArcaEnvironment.Homologation
    : ArcaEnvironment.Production;

var wsaaOptions = new WsaaOptions
{
    Environment = environment
};

var wsfeOptions = new Wsfev1Options
{
    Environment = environment
};
```

Notas:

- homologacion y produccion usan endpoints distintos
- tambien requieren certificado, autorizacion y punto de venta correctos para ese ambiente
- las keys de persistencia de access tickets ya discriminan por ambiente

## Todavia no implementado intencionalmente

- verificacion end-to-end de WSAA con certificados reales de produccion
- verificacion end-to-end de WSFEv1 productivo con credenciales reales
- proxies WSDL
- rendering de PDF/HTML/ticket

## Integration tests de homologacion

- son opt-in
- quedan `skipped` por defecto durante `dotnet test`
- hoy cubren:
  - obtencion de access ticket WSAA para `wsfe`
  - `GetLastAuthorizedNumberAsync`
  - `GetInvoiceAsync` opcional contra un comprobante conocido
  - emision real opt-in de:
    - `Factura B`
    - `Factura A`
    - `Nota de Credito B`
    - `Nota de Credito A`

Variables de entorno:

- `ARCANET_RUN_HOMOLOGATION_TESTS=true`
- `ARCANET_RUN_HOMOLOGATION_ISSUANCE_TESTS=true`
- `ARCANET_RUN_POSTGRES_INTEGRATION_TESTS=true`
- `ARCANET_TEST_POSTGRES_IMAGE`
- `ARCANET_TEST_CUIT`
- `ARCANET_TEST_CERTIFICATE_PATH`
- `ARCANET_TEST_CERTIFICATE_PASSWORD`
- `ARCANET_TEST_POINT_OF_SALE`
- `ARCANET_TEST_ACCESS_TICKET_STORE_PATH`
- `ARCANET_TEST_VOUCHER_TYPE`
- `ARCANET_TEST_VOUCHER_TYPE_NAME`
- `ARCANET_TEST_EXISTING_VOUCHER_NUMBER`
- `ARCANET_TEST_HTTP_TIMEOUT_SECONDS`

Ejemplo de sesion PowerShell:

```powershell
$env:ARCANET_RUN_HOMOLOGATION_TESTS = "true"
$env:ARCANET_RUN_HOMOLOGATION_ISSUANCE_TESTS = "true"
$env:ARCANET_TEST_CUIT = "20123456789"
$env:ARCANET_TEST_CERTIFICATE_PATH = "C:\secrets\arca-homo.pfx"
$env:ARCANET_TEST_CERTIFICATE_PASSWORD = "local-only-secret"
$env:ARCANET_TEST_POINT_OF_SALE = "5"
$env:ARCANET_TEST_ACCESS_TICKET_STORE_PATH = "C:\tmp\arcanet-homo-access-tickets"
$env:ARCANET_TEST_VOUCHER_TYPE = "6"
$env:ARCANET_TEST_VOUCHER_TYPE_NAME = "Factura B"
$env:ARCANET_TEST_EXISTING_VOUCHER_NUMBER = "1234"
dotnet test --filter "Category=Integration"
```

Integration tests de PostgreSQL para contribuidores:

```powershell
$env:ARCANET_RUN_POSTGRES_INTEGRATION_TESTS = "true"
dotnet test --filter "Category=Integration"
```

Notas:

- estos tests usan `Testcontainers` y requieren Docker
- validan el modulo opcional PostgreSQL contra una instancia real
- no son necesarios para consumir la libreria desde una app

## Notas operativas

- no commitees certificados, passwords, tokens ni CUIT de terceros
- mantenelos en secretos locales o variables de entorno
- los tests de homologacion usan `FileAccessTicketStore` durable para recuperar un `TA` vigente entre corridas
- la suite smoke es intencionalmente de solo lectura
- la suite de emision real esta detras de un segundo flag explicito porque genera comprobantes reales en homologacion

## Recuperacion ante resultados inciertos

Si `CreateInvoiceAsync` devuelve `UnknownInvoiceResult`, el siguiente paso conservador es consultar ese mismo `PtoVta + Tipo + Numero` antes de decidir si un retry es seguro.

```csharp
using ARCANet.Invoices;

if (result is UnknownInvoiceResult unknown)
{
    var recovery = new InvoiceSubmissionRecovery(invoiceClient);
    var reconciliation = await recovery.ReconcileAsync(unknown, cancellationToken);

    if (reconciliation is AuthorizedInvoiceReconciliationResult authorized)
    {
        Console.WriteLine(authorized.Invoice.AuthorizationCode);
    }
}
```

Notas:

- `InvoiceSubmissionRecovery` solo hace la consulta post-error y clasifica "autorizado" vs "todavia no confirmado"
- no reserva numeros, no persiste intentos y no decide retries automaticos por tu app
- numeracion e idempotencia siguen siendo responsabilidad de la app

## Helpers para notas de credito

Si ya tenes la factura original autorizada, la forma recomendada de construir una nota de credito es derivarla desde ese comprobante en lugar de reconstruir a mano todos los campos asociados.

```csharp
using ARCANet.Invoices;

var request = CreditNoteRequestFactory.CreateFullCancellation(
    originalInvoice,
    voucherNumber: nextNumber,
    issueDate: today,
    externalIdempotencyKey: $"refund:{refundId}:credit-note");
```

Para ajustes parciales:

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
    ]);
```

Notas:

- el mapping automatico actual soporta `Factura A -> Nota de Credito A` y `Factura B -> Nota de Credito B`
- el helper preserva receptor, moneda, concepto y comprobante asociado desde la factura original
- para notas parciales, exige totales y desglose de impuestos explicitos, y valida que no excedan a la factura original

## Generacion de imagen QR

`ARCANet` puede generar el QR fiscal no solo como payload/URL, sino tambien como contenido de imagen para embeber en el template del POS.

```csharp
using ARCANet.Qr;

var qrGenerator = new ArcaQrGenerator();

string svg = qrGenerator.BuildSvg(authorizedInvoice);
byte[] png = qrGenerator.BuildPng(authorizedInvoice);
```

Notas:

- esto solo genera el QR fiscal
- no renderiza el comprobante completo ni el ticket
- el POS sigue siendo responsable del layout final PDF/HTML/ticket

## Uso de validacion

```csharp
using ARCANet;
using ARCANet.Invoices;

var validator = new InvoiceRequestValidator(new SystemClock());

CreateInvoiceRequest request = new()
{
    IssuerCuit = 20304050607,
    VoucherType = new VoucherType(1, "Factura A"),
    PointOfSale = 5,
    VoucherNumber = 1234,
    Concept = InvoiceConcept.Products,
    IssueDate = new DateOnly(2026, 5, 14),
    Customer = new CustomerIdentity
    {
        Name = "Cliente SA",
        DocumentTypeCode = 80,
        DocumentNumber = "30712345678"
    },
    ReceiverVatCondition = new ReceiverVatCondition(1, "IVA Responsable Inscripto"),
    Totals = new MoneyTotals
    {
        TotalAmount = 1210.00m,
        TaxableAmount = 1000.00m,
        VatAmount = 210.00m
    },
    Currency = new CurrencyAmount("PES", 1.00m)
};

InvoiceValidationResult validation = validator.Validate(request);

if (!validation.IsValid)
{
    foreach (var error in validation.Errors)
    {
        Console.WriteLine($"{error.Code}: {error.Message}");
    }
}
```

## Forma de uso objetivo

Esta es la forma objetivo de la API publica. La orquestacion existe, pero no debe interpretarse como lista para produccion solo por eso.

```csharp
using ARCANet.Abstractions;
using ARCANet.Invoices;

CreateInvoiceResult result = await arcaClient.Invoices.CreateInvoiceAsync(request, cancellationToken);

if (result is AuthorizedInvoiceResult authorized)
{
    Console.WriteLine(authorized.Invoice.AuthorizationCode);
    Console.WriteLine(authorized.Invoice.QrUrl);
}
```

## Documentacion

- [Documento de investigacion](docs/arca-afip-research.md)
- [Lista de verificacion de cumplimiento](docs/compliance-checklist.md)
- [Configuracion de homologacion](docs/homologation-setup.md)
- [Persistencia de access tickets](docs/access-ticket-persistence.md)
- [Uso de notas de credito](docs/credit-note-usage.md)
- [Numeracion y recuperacion para POS](docs/pos-numbering-and-recovery.md)
- [Plan de readiness para POS](docs/pos-readiness-plan.md)
