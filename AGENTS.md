# AGENTS.md

## Project goal

`ARCANet` is an open source .NET library for ARCA/AFIP focused on real-world integration in Argentine applications, especially `POS` systems.

The current focus of the project is:

- WSAA
- WSFEv1
- fiscal QR
- taxpayer registry lookup by CUIT
- access ticket persistence
- integration helpers for real invoicing scenarios

The core is not intended to:

- expose raw SOAP/WSDL models as the public API
- mix rendering concerns into the fiscal core
- solve numbering, stock, cash register, or the full commercial workflow of the POS

## Responsibility boundary

`ARCANet` is responsible for:

- WSAA authentication
- WSFEv1 invoice issuance and lookup
- typed fiscal results
- fiscal QR generation
- taxpayer registry lookup
- recovery from uncertain submission states
- fiscal credit note helpers

The POS is responsible for:

- invoice numbering and reservation
- persistence of sales and business-side invoices
- business idempotency
- UI and business workflow
- stock, cash register, customers, and payments
- final PDF/HTML/ticket templates, unless optional rendering modules are added

## Repository conventions

- explanatory project documentation stays in Spanish
- code, type names, and public API stay in technical English
- use `Conventional Commits`
- use one branch per change
- open PRs into `main`
- do not mix large feature work with unrelated CI or docs changes

## Design criteria

- prefer typed wrappers over exposing raw SOAP
- prefer neutral models and explicit result types
- keep `Authorized / Rejected / Unknown` states visible
- split optional modules when they add dependencies or separate concerns
- keep the fiscal core usable without rendering
- validate major capabilities against real homologation before considering them done

## Testing

- `dotnet test ARCANet.slnx --configuration Release` should pass before closing a change
- homologation and real PostgreSQL tests are opt-in
- when ARCA produces a timeout or uncertain real-world state, prefer reconciliation over assuming a definitive failure

## Current relevant state

Already implemented:

- WSAA
- WSFEv1
- `Factura A/B`
- `Nota de Credito A/B`
- fiscal QR as payload/URL/SVG/PNG
- `PostgresAccessTicketStore`
- taxpayer registry lookup via `ws_sr_constancia_inscripcion`
- recovery from `UnknownInvoiceResult`

Important pending areas:

- generic invoice rendering
- complete XML documentation for the public API before `1.0`
- general integration ergonomics improvements

## If working on rendering

Key principle:

- rendering must stay separate from the fiscal core

Recommended first stage:

- non-customizable generic module
- HTML first
- PDF later if needed

The rendering model should accept:

- `AuthorizedInvoice`
- extra issuer display data
- commercial line-item details, which do not currently live in the fiscal core

Do not assume that `CreateInvoiceRequest` or `AuthorizedInvoice` alone contain all the visual information needed for a useful POS ticket.
