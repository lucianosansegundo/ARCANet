# ARCANet

Open source .NET library for ARCA/AFIP focused on a safer, higher-level API for real applications.

Initial scope:

- WSAA authentication behind the scenes
- WSFEv1 invoicing behind a domain-oriented API
- Homologation/testing first
- Factura A/B and Nota de Credito A/B
- Voucher lookup and last authorized number helpers
- Fiscal QR payload/URL generation
- Local request validation, internal neutral mapping, and explicit result modeling

Non-goals for the initial core:

- exposing SOAP/WSDL models as the public API
- PDF, HTML or thermal ticket rendering
- QR image generation
- visual invoice templates

Design goals:

- hide `token`, `sign`, `FECAESolicitar` and SOAP classes from consumers
- keep the SDK generic and reusable
- separate fiscal core concerns from rendering concerns
- model authorized, rejected, observed and uncertain outcomes explicitly

Current status:

- public API scaffolding
- local invoice validation
- internal transport-neutral invoice submission mapping
- WSAA core pieces implemented without WSDL proxies
- WSFEv1 core pieces implemented without WSDL proxies
- public `InvoiceClient` orchestration over validation + WSAA + WSFEv1 + QR
- fiscal QR payload/JSON/Base64/URL generation
- unit tests for validation, mapping, QR, WSAA, WSFEv1, and invoice orchestration
- consulted invoice reconstruction from `FECompConsultar` for fiscal fields returned by ARCA
- not production-ready

Important:

- ARCANet does not replace professional accounting or tax validation
- mutable regulatory thresholds and rules should be treated as configurable, not hardcoded business truths
- `FECompConsultar` does not return every business/presentation field; callers should persist receiver display data and any additional audit data they need

Implemented now:

- `CreateInvoiceRequest` and result models
- `InvoiceRequestValidator`
- internal `InternalInvoiceSubmission` mapping
- `ArcaQrGenerator`
- `WsaaAccessTicketProvider`
- `Wsfev1Client`
- `InvoiceClient`
- raw SOAP transport via `HttpClientSoapTransport`

Intentionally not implemented yet:

- end-to-end verified WSAA authentication with real certificates
- end-to-end verified WSFEv1 homologation flow with real credentials/certificates
- WSDL proxies
- PDF/HTML/ticket rendering
- QR image generation

Homologation integration tests:

- opt-in only
- skipped by default during `dotnet test`
- limited to smoke coverage without issuing new vouchers
- currently cover:
  - WSAA access ticket retrieval for `wsfe`
  - `GetLastAuthorizedNumberAsync`
  - optional `GetInvoiceAsync` against a known voucher number

Environment variables:

- `ARCANET_RUN_HOMOLOGATION_TESTS=true`
- `ARCANET_TEST_CUIT`
- `ARCANET_TEST_CERTIFICATE_PATH`
- `ARCANET_TEST_CERTIFICATE_PASSWORD`
- `ARCANET_TEST_POINT_OF_SALE`
- `ARCANET_TEST_VOUCHER_TYPE`
- `ARCANET_TEST_VOUCHER_TYPE_NAME`
- `ARCANET_TEST_EXISTING_VOUCHER_NUMBER`
- `ARCANET_TEST_HTTP_TIMEOUT_SECONDS`

Example PowerShell session:

```powershell
$env:ARCANET_RUN_HOMOLOGATION_TESTS = "true"
$env:ARCANET_TEST_CUIT = "20123456789"
$env:ARCANET_TEST_CERTIFICATE_PATH = "C:\secrets\arca-homo.pfx"
$env:ARCANET_TEST_CERTIFICATE_PASSWORD = "local-only-secret"
$env:ARCANET_TEST_POINT_OF_SALE = "5"
$env:ARCANET_TEST_VOUCHER_TYPE = "6"
$env:ARCANET_TEST_VOUCHER_TYPE_NAME = "Factura B"
$env:ARCANET_TEST_EXISTING_VOUCHER_NUMBER = "1234"
dotnet test --filter "Category=Integration"
```

Operational notes:

- do not commit certificates, passwords, tokens or third-party CUITs
- keep these values in local secrets or environment variables only
- the integration suite is intentionally read-only; it does not call `CreateInvoiceAsync`
- issuing real homologation vouchers should remain a deliberate manual step until the team chooses an explicit issuance test strategy

Validation usage:

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

Future intended usage:

This is the intended public API shape. The orchestration exists, but it is not yet verified end-to-end against homologation with real credentials/certificates.

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

Documentation:

- [Research document](docs/arca-afip-research.md)
- [Compliance checklist](docs/compliance-checklist.md)
- [Homologation setup](docs/homologation-setup.md)
