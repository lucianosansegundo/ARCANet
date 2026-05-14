# Checklist de cumplimiento fiscal/técnico para el MVP

Fecha del relevamiento: 2026-05-14

Este checklist resume lo que no deberia escaparse antes de emitir comprobantes reales con ARCANet.

Nota importante:

- ARCANet no reemplaza validacion contable/fiscal profesional
- montos, umbrales y reglas normativas variables no deben quedar hardcodeados como supuestos permanentes del core

Fuentes base:

- https://www.afip.gob.ar/fe/emision-autorizacion/datos-comprobantes.asp
- https://www.afip.gob.ar/fe/emision-autorizacion/consideraciones.asp
- https://www.afip.gob.ar/fe/qr/
- https://www.afip.gob.ar/fe/qr/documentos/QRespecificaciones.pdf
- https://www.afip.gob.ar/fe/ayuda/documentos/wsfev1-RG-4291.pdf
- https://serviciosweb.afip.gob.ar/facturacion/comprobantes/tipos.asp

## Confirmado por documentacion oficial

- WSAA requiere `LoginTicketRequest` firmado y entrega `token/sign` para invocar WSFEv1.
- WSFEv1 expone `FECompUltimoAutorizado`, `FECAESolicitar` y `FECompConsultar` como operaciones clave para el alcance inicial.
- WSFEv1 devuelve datos de autorizacion y permite consultar comprobantes emitidos.
- El comprobante debe contener los datos establecidos en el `Anexo II de la RG 1415/2003`.
- El QR fiscal es obligatorio para comprobantes electronicos emitidos bajo RG `4291/2018`.
- El QR debe construirse como `{URL}?p={DATOS_CMP_BASE64}`.
- La URL publicada en la especificacion del QR es `https://www.arca.gob.ar/fe/qr/`.
- El JSON del QR incluye como minimo:
  - version
  - fecha
  - CUIT emisor
  - punto de venta
  - tipo de comprobante
  - numero de comprobante
  - importe total
  - moneda
  - cotizacion
  - documento receptor cuando corresponda
  - tipo de codigo de autorizacion
  - codigo de autorizacion
- El SDK debe exponer `CAE` y vencimiento del `CAE`.
- `FECompConsultar` devuelve:
  - resultado
  - codigo de autorizacion
  - tipo de emision
  - fecha de vencimiento
  - fecha de proceso
  - observaciones
- La condicion IVA del receptor tiene validaciones oficiales en la documentacion actual de WSFEv1.
- Para consumidor final:
  - debe incluirse la leyenda `A CONSUMIDOR FINAL`
  - si el importe es igual o superior a `10.000.000`, debe informarse identificacion del receptor
- Ese umbral debe tratarse como referencia documental vigente y no como constante normativa inmutable dentro del SDK.
- Los datos de apellido, nombre y domicilio del comprador pueden completarse con `NR` o ceros cuando el sistema lo requiera.
- Las notas de credito/debito deben estar relacionadas con comprobantes emitidos previamente.
- Solo quien emitio el comprobante original puede emitir notas de credito/debito asociadas.
- Los puntos de venta para Web Services deben habilitarse y no deben mezclarse con otros canales de emision.

## Pendiente de validacion fiscal/contable

- alcance exacto de datos minimos exigibles por tipo de receptor segun casuistica real
- reglas exactas para consumidor final en escenarios especiales, por actividad o regimen
- criterios operativos de uso de Factura C y Factura M para el roadmap
- reglas completas para anulacion total vs ajuste parcial mediante nota de credito
- campos visuales/leyendas adicionales exigibles en la representacion impresa o digital segun tipo de comprobante
- necesidad de datos adicionales del emisor/receptor para ciertos regimens especiales
- reglas de conservacion documental y auditoria fuera del flujo tecnico del web service
- obligaciones especificas de duplicados electronicos segun actividad y modalidad de emision

## Fuera del MVP pero relevante

- CAEA
- contingencias y metodos de resguardo
- rendering PDF/HTML/ticket
- imagen QR
- templates visuales customizables
- WSMTXCA
- soporte completo de comprobantes `C`, `M`, `E`, FCE y regimens especiales
- cache distribuido multi-instancia de access tickets
- automatizacion de tramites administrativos de alta

## Criterios operativos para no fallar en el MVP

- No hardcodear CUIT, punto de venta, URLs ni certificados.
- No commitear certificados, claves, tokens ni secretos.
- No hacer retry ciego de `FECAESolicitar`.
- Si el resultado es incierto, consultar `FECompConsultar` antes de reintentar.
- La aplicacion consumidora debe manejar lock/transaccion para numeracion.
- El SDK no debe ser el duenio final de la numeracion global.
- El core debe poder generar payload/URL del QR, aunque no renderice el comprobante.
- El core debe devolver todos los datos necesarios para persistencia, auditoria y representacion posterior.
