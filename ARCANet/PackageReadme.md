# ARCANet

`ARCANet` es una libreria open source .NET para ARCA/AFIP enfocada en una API mas segura y de mas alto nivel para aplicaciones reales.

Alcance actual del paquete principal:

- autenticacion WSAA
- emision y consulta WSFEv1
- generacion de QR como payload/URL/SVG/PNG
- modelado explicito de resultados autorizados, rechazados e inciertos
- flujo orientado primero a homologacion

Flujos de homologacion ya validados:

- Factura A
- Factura B
- Nota de Credito A
- Nota de Credito B

Boundaries importantes:

- la numeracion y reserva de comprobantes siguen siendo responsabilidad de la app consumidora
- la persistencia del comprobante de negocio sigue siendo responsabilidad de la app consumidora
- `ARCANet` no renderiza PDFs ni tickets completos

Paquete relacionado:

- `ARCANet.Persistence.Postgres`

La documentacion principal del repositorio incluye:

- configuracion de homologacion
- persistencia de access tickets
- uso de notas de credito
- numeracion y recuperacion para POS
