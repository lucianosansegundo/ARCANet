# ARCANet

`ARCANet` is an open source .NET library for ARCA/AFIP focused on a safer, higher-level API for real applications.

Current core scope:

- WSAA authentication
- WSFEv1 invoice issuance and lookup
- QR payload/URL/SVG/PNG generation
- explicit result modeling for authorized, rejected, and uncertain outcomes
- homologation-first workflow

Current validated homologation flows:

- Factura A
- Factura B
- Nota de Credito A
- Nota de Credito B

Important boundaries:

- numbering and voucher reservation stay in the consuming application
- persistence of business invoices stays in the consuming application
- `ARCANet` does not render full invoice PDFs or tickets

Related package:

- `ARCANet.Persistence.Postgres`

Main repository documentation includes:

- homologation setup
- access ticket persistence
- credit note usage
- POS numbering and recovery guidance
