# ARCANet

Librería open source .NET para ARCA/AFIP enfocada en una API más segura y de más alto nivel para aplicaciones reales.

Alcance inicial:

- WSAA authentication behind the scenes
- WSFEv1 invoicing behind a domain-oriented API
- Homologation/testing first
- Factura A/B and Nota de Credito A/B
- Voucher lookup and last authorized number helpers
- Fiscal QR payload/URL generation
- Local request validation, internal neutral mapping, and explicit result modeling

No objetivos del core inicial:

- exposing SOAP/WSDL models as the public API
- PDF, HTML or thermal ticket rendering
- QR image generation
- visual invoice templates

Objetivos de diseño:

- hide `token`, `sign`, `FECAESolicitar` and SOAP classes from consumers
- keep the SDK generic and reusable
- separate fiscal core concerns from rendering concerns
- model authorized, rejected, observed and uncertain outcomes explicitly

Estado actual:

- public API scaffolding
- local invoice validation
- internal transport-neutral invoice submission mapping
- WSAA core pieces implemented without WSDL proxies
- WSFEv1 core pieces implemented without WSDL proxies
- public `InvoiceClient` orchestration over validation + WSAA + WSFEv1 + QR
- fiscal QR payload/JSON/Base64/URL/SVG/PNG generation
- unit tests for validation, mapping, QR, WSAA, WSFEv1, and invoice orchestration
- consulted invoice reconstruction from `FECompConsultar` for fiscal fields returned by ARCA
- Phase 1 access ticket persistence contracts and store-backed WSAA reuse
- homologation real issuance validated for Factura A/B and Nota de Credito A/B
- not production-ready

Importante:

- ARCANet does not replace professional accounting or tax validation
- mutable regulatory thresholds and rules should be treated as configurable, not hardcoded business truths
- `FECompConsultar` does not return every business/presentation field; callers should persist receiver display data and any additional audit data they need
- access ticket reuse defaults to an in-memory `IAccessTicketStore`; applications can supply a custom durable store without changing the invoice API

Implementado actualmente:

- `CreateInvoiceRequest` and result models
- `InvoiceRequestValidator`
- `CreditNoteRequestFactory`
- internal `InternalInvoiceSubmission` mapping
- `ArcaQrGenerator`
- `WsaaAccessTicketProvider`
- `IAccessTicketStore`, `InMemoryAccessTicketStore`, `NullAccessTicketStore`
- `FileAccessTicketStore`
- `Wsfev1Client`
- `InvoiceClient`
- `InvoiceSubmissionRecovery`
- raw SOAP transport via `HttpClientSoapTransport`

Módulos opcionales de persistencia:

- `ARCANet.Persistence.Postgres`
  - `PostgresAccessTicketStore`
  - `PostgresAccessTicketStoreOptions`
  - `PostgresAccessTicketStore.CreateInitializedAsync(...)`

Persistencia de access tickets en PostgreSQL:

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

See [Access ticket persistence](docs/access-ticket-persistence.md) for store selection guidance and PostgreSQL setup details.

Selección de ambiente:

`ARCANet` already distinguishes between ARCA environments:

- `ArcaEnvironment.Homologation`
- `ArcaEnvironment.Production`

El default actual es `Homologation`.

Para un POS, el mapeo habitual es:

- POS "test mode" => `ArcaEnvironment.Homologation`
- POS "real/live mode" => `ArcaEnvironment.Production`

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

Notes:

- homologation and production use different endpoints
- they also require the correct certificate, authorization, and point-of-sale setup for that environment
- access ticket persistence keys already discriminate by environment, so homologation and production tickets are kept separate

Todavía no implementado intencionalmente:

- end-to-end verified WSAA authentication with real certificates
- end-to-end verified WSFEv1 homologation flow with real credentials/certificates
- WSDL proxies
- PDF/HTML/ticket rendering
- QR image generation

Integration tests de homologación:

- opt-in only
- skipped by default during `dotnet test`
- currently cover:
  - WSAA access ticket retrieval for `wsfe`
  - `GetLastAuthorizedNumberAsync`
  - optional `GetInvoiceAsync` against a known voucher number
  - opt-in real issuance for:
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

Ejemplo de sesión PowerShell:

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

- these tests use `Testcontainers` and require Docker
- they validate the optional PostgreSQL persistence module against a real PostgreSQL instance
- they are not required to consume the library in an application

Notas operativas:

- do not commit certificates, passwords, tokens or third-party CUITs
- keep these values in local secrets or environment variables only
- homologation tests now use a durable `FileAccessTicketStore` so repeated runs can recover a still-valid `TA`
- the smoke suite is intentionally read-only
- issuance tests are opt-in behind a second explicit flag because they generate real homologation vouchers
- issuing real homologation vouchers should remain a deliberate manual step until the team chooses an explicit issuance test strategy

Recuperación ante resultados inciertos:

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

Notes:

- `InvoiceSubmissionRecovery` only performs the post-error lookup and classifies `authorized` vs `still unconfirmed`
- it does not reserve numbers, persist attempts, or decide automatic retries for your app
- numeration and idempotency remain application responsibilities

Helpers para notas de crédito:

Si ya tenés la factura original autorizada, la forma recomendada de construir una nota de crédito es derivarla desde ese comprobante en lugar de reconstruir a mano todos los campos asociados.

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

Notes:

- automatic mapping currently supports `Factura A -> Nota de Credito A` and `Factura B -> Nota de Credito B`
- the helper preserves receiver, currency, concept, and associated voucher data from the original invoice
- partial credit notes require explicit totals and tax breakdown; the helper validates that they do not exceed the original invoice

Generación de imagen QR:

`ARCANet` ya puede generar el QR fiscal no solo como payload/URL, sino también como contenido de imagen para embeber en el template del POS.

```csharp
using ARCANet.Qr;

var qrGenerator = new ArcaQrGenerator();

string svg = qrGenerator.BuildSvg(authorizedInvoice);
byte[] png = qrGenerator.BuildPng(authorizedInvoice);
```

Notes:

- this only generates the fiscal QR
- it does not render the full invoice or ticket
- the POS remains responsible for the final ticket/PDF/HTML layout

Uso de validación:

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

Forma de uso objetivo:

Esta es la forma objetivo de la API pública. La orquestación existe, pero no debe interpretarse como “lista para producción” solo por eso.

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

Documentación:

- [Research document](docs/arca-afip-research.md)
- [Compliance checklist](docs/compliance-checklist.md)
- [Homologation setup](docs/homologation-setup.md)
- [Access ticket persistence](docs/access-ticket-persistence.md)
- [Credit note usage](docs/credit-note-usage.md)
- [POS numbering and recovery](docs/pos-numbering-and-recovery.md)
- [POS readiness plan](docs/pos-readiness-plan.md)
