# ARCANet - Investigacion tecnica inicial ARCA/AFIP

Fecha del relevamiento: 2026-05-14

## Objetivo

Este documento define el marco tecnico inicial para `ARCANet`, una libreria open source .NET orientada a simplificar el uso correcto de ARCA/AFIP desde aplicaciones reales.

Objetivo de producto:

- exponer una API de alto nivel
- ocultar complejidad de WSAA, WSFEv1, SOAP, `token/sign` y modelos del WSDL
- mantener el core generico y reusable
- priorizar homologacion/pruebas antes que produccion
- dejar preparados los bordes para rendering, PDF, QR image y otros servicios futuros

ARCANet no debe ser un wrapper fino de `FECAESolicitar`. Debe ser una abstraccion fiscal/tecnica mas simple, mas segura y mas dificil de usar incorrectamente.

Nota importante:

- ARCANet no reemplaza validacion contable/fiscal profesional
- toda regla normativa mutable, umbral monetario o criterio interpretativo debe tratarse como configurable o revisable, no como verdad hardcodeada del core

## Alcance de este entregable

Este entregable es solo de investigacion y diseno.

Incluye:

- relevamiento oficial WSAA
- relevamiento oficial WSFEv1
- alcance fiscal minimo visible para el MVP
- QR fiscal obligatorio
- propuesta de API publica de alto nivel
- limites entre SDK y aplicacion consumidora
- estrategia de numeracion, concurrencia e idempotencia
- estrategia de pruebas

No incluye:

- implementacion SOAP
- proxies generados
- firma CMS/PKCS#7
- PDF, HTML o ticket rendering
- imagen QR
- paquetes NuGet

## Estado de implementacion actual

Estado real del SDK al cierre de esta iteracion:

- API publica inicial de facturacion definida
- validacion local de `CreateInvoiceRequest` implementada
- modelo interno neutral de envio implementado
- mapping `CreateInvoiceRequest -> InternalInvoiceSubmission` implementado
- nucleo WSAA implementado sin proxies WSDL
- nucleo WSFEv1 implementado sin proxies WSDL
- `InvoiceClient` implementado sobre validacion + WSAA + WSFEv1 + QR
- generacion de QR fiscal implementada

Todavia no implementado:

- verificacion end-to-end de WSAA con credenciales reales
- verificacion end-to-end de WSFEv1 en homologacion con credenciales reales
- proxies WSDL
- rendering

## Fuentes oficiales consultadas

Fuentes principales:

1. Arquitectura general de web services
   - https://ftp.afip.gob.ar/ws/documentacion/arquitectura-general.asp
2. WSAA - pagina oficial
   - https://www.afip.gob.ar/ws/documentacion/wsaa.asp
3. WSAA - Especificacion Tecnica 1.2.2
   - https://www.afip.gob.ar/ws/WSAA/Especificacion_Tecnica_WSAA_1.2.2.pdf
4. WSAA - Manual para el desarrollador
   - https://www.afip.gob.ar/ws/WSAA/WSAAmanualDev.pdf
5. WSAA - WSDL homologacion
   - https://wsaahomo.afip.gov.ar/ws/services/LoginCms?WSDL
6. WSAA - WSDL produccion
   - https://wsaa.afip.gov.ar/ws/services/LoginCms?WSDL
7. Administracion de certificados y relaciones
   - https://www.afip.gob.ar/ws/WSAA/ADMINREL.DelegarWS.pdf
8. Factura electronica - micrositio oficial
   - https://www.afip.gob.ar/fe/
9. WSFEv1 - Manual para el desarrollador
   - https://www.afip.gob.ar/fe/ayuda/documentos/wsfev1-RG-4291.pdf
10. Web services de factura electronica
   - https://www.afip.gob.ar/ws/documentacion/ws-factura-electronica.asp
11. Datos de los comprobantes
   - https://www.afip.gob.ar/fe/emision-autorizacion/datos-comprobantes.asp
12. Consideraciones para la solicitud
   - https://www.afip.gob.ar/fe/emision-autorizacion/consideraciones.asp
13. Codigo QR - conceptos generales
   - https://www.afip.gob.ar/fe/qr/
14. Codigo QR - especificaciones
   - https://www.afip.gob.ar/fe/qr/documentos/QRespecificaciones.pdf
15. Comprobantes - tipos y datos
   - https://serviciosweb.afip.gob.ar/facturacion/comprobantes/tipos.asp
16. Constatacion de comprobantes con CAE
   - https://servicioscf.afip.gob.ar/publico/comprobantes/cae.aspx
17. WSMTXCA - Manual para el desarrollador
   - https://www.afip.gob.ar/fe/ayuda/documentos/wsmtxca-RG2904.pdf

Notas del relevamiento:

- la documentacion de WSFEv1 vigente al momento del relevamiento incorpora validaciones asociadas a `CondicionIVAReceptorId` por RG `5616/2024`
- el micrositio oficial de QR publica una especificacion tecnica independiente y vigente
- el micrositio de Factura Electronica remite a RG `1415/2003` para los datos obligatorios del comprobante

## Resumen ejecutivo y decision recomendada

Recomendacion para el MVP:

- comenzar con `WSAA + WSFEv1`
- apuntar primero a `Homologation`
- soportar inicialmente:
  - Factura A
  - Factura B
  - Nota de Credito A
  - Nota de Credito B
- incorporar en el core o en un modulo liviano del SDK:
  - generacion de payload QR
  - serializacion JSON/Base64 del QR
  - URL final del QR fiscal
- dejar fuera del core inicial:
  - PDF
  - HTML de factura
  - ticket termico
  - imagen QR
  - templates visuales

Motivo:

- `WSFEv1` es suficiente para un MVP sin detalle de items
- el valor del SDK no debe estar en exponer `FECAESolicitar`, sino en resolver autenticacion, mapeos, reglas tecnicas, errores, QR e integracion segura
- el mayor riesgo de uso incorrecto no esta en SOAP sino en numeracion, reintentos, estado incierto, datos fiscales minimos y representacion posterior

## Principios de producto

ARCANet deberia cumplir estos principios:

- API publica estable y orientada a negocio, no a WSDL
- separacion estricta entre modelos internos SOAP y contratos publicos
- configuracion explicita por ambiente
- cero hardcode de CUIT, puntos de venta, certificados, secretos o URLs
- validaciones tecnicas fuertes antes de invocar ARCA
- resultados funcionales ricos; excepciones solo para fallos tecnicos
- soporte para generar todos los datos que una app necesita para una representacion valida, aun cuando el SDK no renderice el comprobante
- validacion local configurable antes de cualquier llamada a ARCA
- modelo interno neutral estable antes de definir transporte SOAP

## Flujo completo de alto nivel

1. La aplicacion consumidora prepara la solicitud de comprobante.
2. La aplicacion reserva internamente un numero o prepara la emision bajo lock/transaccion.
3. El SDK obtiene un ticket de acceso WSAA para `wsfe`.
4. El SDK invoca WSFEv1 con modelos propios internos.
5. El SDK traduce la respuesta oficial a un resultado de alto nivel.
6. Si hubo autorizacion con CAE:
   - devuelve datos fiscales completos
   - devuelve CAE y vencimiento
   - devuelve datos necesarios para QR
   - puede devolver observaciones
7. Si hubo rechazo funcional:
   - devuelve rechazo con codigos y mensajes oficiales mapeados
8. Si hubo error tecnico incierto:
   - devuelve resultado incierto y helpers/datos para consulta
9. La aplicacion persiste el resultado y genera representacion visual si corresponde.

Nota operativa importante:

- `FECompConsultar` es clave para resolver estados inciertos e idempotencia
- pero no devuelve necesariamente todos los datos de negocio/presentacion que una aplicacion podria necesitar luego
- por eso la aplicacion consumidora debe persistir su propio snapshot de emision y no depender de la consulta posterior para reconstruir todo el comprobante visual

## 1. WSAA

### 1.1 Flujo de autenticacion

WSAA expone `loginCms`.

Flujo:

1. generar `LoginTicketRequest`
2. informar:
   - `uniqueId`
   - `generationTime`
   - `expirationTime`
   - `service`
3. firmar el XML en CMS/PKCS#7
4. invocar `loginCms`
5. parsear `LoginTicketResponse`
6. extraer:
   - `token`
   - `sign`
   - ventana de vigencia

Para WSFEv1, el `service` del TRA es `wsfe`.

### 1.2 Certificados

Requisitos tecnicos:

- certificado X.509 valido
- clave privada asociada
- certificado habilitado para el CUIT representado
- relacion/autorizacion del web service correspondiente

Diferencias entre homologacion y produccion:

- endpoints distintos
- certificados distintos
- relaciones y habilitaciones independientes
- no debe asumirse que un certificado o habilitacion de homologacion sirve en produccion

### 1.3 Endpoints WSAA

Homologacion:

- `https://wsaahomo.afip.gov.ar/ws/services/LoginCms`
- `https://wsaahomo.afip.gov.ar/ws/services/LoginCms?WSDL`

Produccion:

- `https://wsaa.afip.gov.ar/ws/services/LoginCms`
- `https://wsaa.afip.gov.ar/ws/services/LoginCms?WSDL`

### 1.4 Implicancias para el SDK

ARCANet deberia:

- encapsular completamente `token` y `sign`
- renovar credenciales antes de vencer
- tolerar drift horario defensivamente
- no loguear secretos
- desacoplar la obtencion del certificado con `ICertificateProvider`
- desacoplar la cache del access ticket con `IAccessTicketProvider`

## 2. Certificados y alta administrativa

Circuito esperado, segun documentacion oficial:

1. generar CSR y par de claves
2. dar de alta certificado digital en el entorno correspondiente
3. asociar el certificado al CUIT
4. habilitar o delegar el servicio
5. usar ese certificado para firmar el TRA

WSASS:

- aparece como parte del ecosistema de seguridad/administracion
- no se detecta necesidad de incorporarlo como API funcional del MVP
- se documenta como contexto administrativo, no como parte del core

Pendiente de validacion fiscal/contable:

- confirmar checklists administrativos exactos vigentes para pase de homologacion a produccion

## 3. WSFEv1

### 3.1 Rol de WSFEv1 en ARCANet

`wsfev1` es el servicio recomendado para el MVP porque cubre comprobantes `A`, `B`, `C` y `M` sin detalle de item y permite `CAE` y `CAEA` para determinados casos.

Para el primer alcance, ARCANet deberia concentrarse en:

- `FECompUltimoAutorizado`
- `FECAESolicitar`
- `FECompConsultar`
- tablas parametricas necesarias

### 3.2 Operaciones principales

`FECompUltimoAutorizado`

- consulta el ultimo numero autorizado por `PtoVta + CbteTipo`
- sirve como helper operativo
- no resuelve por si solo concurrencia ni idempotencia

`FECAESolicitar`

- solicita autorizacion CAE
- devuelve aprobacion, rechazo u observaciones
- la respuesta aprobada incluye `CAE` y `CAEFchVto`

`FECompConsultar`

- consulta un comprobante ya emitido por tipo, numero y punto de venta
- devuelve los datos del comprobante y el tipo de emision
- es clave para resolver estados inciertos

### 3.3 Endpoints WSFEv1

Homologacion:

- `https://wswhomo.afip.gov.ar/wsfev1/service.asmx`
- `https://wswhomo.afip.gov.ar/wsfev1/service.asmx?WSDL`

Produccion:

- `https://servicios1.afip.gov.ar/wsfev1/service.asmx`
- `https://servicios1.afip.gov.ar/wsfev1/service.asmx?WSDL`

### 3.4 Tablas parametricas relevantes

Para el MVP, como minimo:

- `FEParamGetTiposCbte`
- `FEParamGetTiposDoc`
- `FEParamGetTiposIva`
- `FEParamGetTiposMonedas`
- `FEParamGetCotizacion`
- `FEParamGetPtosVenta`
- `FEParamGetTiposConcepto`
- `FEParamGetTiposTributos`
- `FEParamGetTiposOpcional`
- `FEParamGetCondicionIvaReceptor`

Relevancia nueva:

- la documentacion actual de WSFEv1 publica validaciones que hacen obligatorio `CondicionIVAReceptorId` segun RG `5616`
- no conviene dejar esa informacion fuera del modelo publico del SDK

## 4. Alcance fiscal obligatorio visible para el SDK

### 4.1 Distincion clave

Hay dos planos diferentes:

1. Lo que ARCA exige tecnicamente por web service.
2. Lo que la normativa exige para que el comprobante emitido y su representacion sean correctos.

ARCANet debe modelar ambos, pero sin fingir que toda regla normativa puede decidirse solo con validacion tecnica local.

### 4.2 Base oficial minima

El micrositio oficial de Factura Electronica indica que los comprobantes deben contener los datos establecidos en el `Anexo II de la RG 1415/2003`.

Eso implica que el SDK no debe limitarse a los campos minimos del request SOAP. Debe exponer, luego de la autorizacion, todos los datos que una aplicacion necesita para:

- persistir
- auditar
- representar visualmente
- constatar el comprobante
- generar QR

### 4.3 Obligaciones minimas que el core debe contemplar

ARCANet debe contemplar, al menos, estos grupos de datos:

- emisor
- receptor
- tipo de comprobante
- punto de venta
- numero de comprobante
- fecha de emision
- concepto
- moneda y cotizacion
- importes
- IVA
- tributos
- importes exentos/no gravados
- comprobantes asociados si corresponde
- CAE y vencimiento
- observaciones
- datos del QR fiscal

## 5. Obligaciones minimas del comprobante electronico

### 5.1 Tipo de comprobante

Para el MVP inicial:

- Factura A
- Factura B
- Nota de Credito A
- Nota de Credito B

Conviene dejar preparada la arquitectura para:

- Factura C
- Nota de Credito C
- Factura M
- Nota de Credito M

Pendiente de validacion fiscal/contable:

- reglas completas de emision y uso practico de `C` y `M` segun condicion real del emisor

### 5.2 CAE y vencimiento del CAE

WSFEv1 devuelve `CAE` y `CAEFchVto` en autorizaciones con CAE.

Implicancias:

- la respuesta autorizada del SDK debe exponer ambos campos siempre
- la aplicacion consumidora debe poder persistirlos
- esos datos deben quedar disponibles para representacion y constatacion

### 5.3 Numeracion

El numero de comprobante:

- no debe ser inventado luego de recibir la respuesta
- debe participar de la estrategia transaccional de la aplicacion
- debe ser consistente con `PtoVta + CbteTipo`

### 5.4 Identificacion del receptor

El receptor debe modelarse con:

- tipo de documento
- numero de documento
- denominacion
- condicion frente al IVA del receptor

La documentacion actual de WSFEv1 incluye validaciones para:

- compatibilidad de `DocTipo` segun clase de comprobante
- obligatoriedad/validez de `CondicionIVAReceptorId`

### 5.5 Consumidor final

Segun el micrositio oficial de Factura Electronica:

- debe incluirse la leyenda `A CONSUMIDOR FINAL`
- si el importe de la operacion es igual o superior a `10.000.000` pesos:
  - debe informarse `CUIT`, `CUIL`, `CDI` o `DNI`
  - para extranjeros, documento/cedula/pasaporte del pais de origen

El mismo micrositio aclara que apellido, nombre y domicilio pueden informarse, o completarse con `NR` o ceros, cuando el sistema lo requiera.

Nota de diseno:

- ese umbral monetario surge de documentacion oficial vigente al momento del relevamiento
- no debe hardcodearse como regla inmutable de negocio
- si ARCANet incorpora helpers o validaciones sobre estos casos, deberian ser configurables o versionables

Pendiente de validacion fiscal/contable:

- confirmar si existen otras condiciones vigentes por monto, actividad o regimen especial que alteren el criterio de identificacion de consumidor final

### 5.6 Condicion fiscal del receptor

La condicion IVA del receptor ya no debe tratarse como dato accesorio. La documentacion actual de WSFEv1 publica validaciones excluyentes y no excluyentes sobre `CondicionIVAReceptorId`.

Recomendacion:

- modelar este dato de forma explicita en la API publica
- no derivarlo implicitamente desde el tipo de documento

### 5.7 Fechas obligatorias

Segun WSFEv1 y la naturaleza del comprobante:

- `CbteFch` es obligatoria
- `FchServDesde` y `FchServHasta` corresponden cuando `Concepto` es servicios o productos y servicios
- `FchVtoPago` puede corresponder segun el caso

Pendiente de validacion fiscal/contable:

- reglas operativas exactas de emision por tipo de operacion y fecha limite normativa

### 5.8 Moneda y cotizacion

El comprobante debe incluir:

- `MonId`
- `MonCotiz`

El QR fiscal tambien exige:

- moneda
- cotizacion en pesos

La documentacion de WSFEv1 publica controles sobre cotizacion y referencia a `FEParamGetCotizacion`.

### 5.9 IVA, exentos, no gravados y tributos

El SDK debe modelar por separado:

- `ImpNeto`
- `ImpIVA`
- `ImpTrib`
- `ImpOpEx`
- `ImpTotConc`
- `ImpTotal`
- detalle de alicuotas IVA
- detalle de tributos

La documentacion de WSFEv1 publica validaciones sobre:

- `ImpTotal = ImpTotConc + ImpNeto + ImpOpEx + ImpTrib + ImpIVA`
- consistencia entre `ImpIVA` y sumatoria de alicuotas

### 5.10 Notas de credito y anulacion

De la documentacion oficial surge:

- solo quien emitio el comprobante original puede emitir notas de credito/debito relacionadas
- las notas deben relacionarse con comprobantes emitidos previamente
- en las solicitudes deben informarse comprobantes asociados cuando corresponda
- el micrositio oficial aclara que notas de credito/debito se emiten para descuentos, bonificaciones, quitas, devoluciones, rescisiones, intereses u otros ajustes de operaciones originarias

Ademas, el micrositio de consideraciones indica que para notas de credito/debito por WS/applicativo deben usarse los codigos correspondientes y no comprobantes multiproposito.

Pendiente de validacion fiscal/contable:

- criterio exacto para "anulacion" total vs ajuste parcial en cada caso de negocio

### 5.11 Leyendas, opcionales y campos especiales

WSFEv1 soporta `Opcionales` y reglas especiales por regimen, actividad o tipo de comprobante.

Para el MVP:

- no conviene abrir de entrada todos los opcionales
- si un opcional es normativamente obligatorio para un escenario soportado, debe modelarse de forma fuerte
- si no aplica al MVP, debe quedar fuera pero documentado

## 6. QR fiscal obligatorio

### 6.1 Nueva decision de producto

El QR no queda fuera del MVP.

Nueva decision:

- el core inicial no renderiza PDF ni imagen QR
- pero ARCANet si debe poder generar:
  - payload normalizado
  - JSON requerido
  - Base64 requerido
  - URL final del QR fiscal

### 6.2 Base oficial

El micrositio oficial de QR indica que:

- el QR es obligatorio para comprobantes electronicos emitidos bajo RG `4291/2018`
- el texto codificado debe tener formato `{URL}?p={DATOS_CMP_BASE64}`
- la URL publicada en la especificacion es `https://www.arca.gob.ar/fe/qr/`
- `DATOS_CMP_BASE64` es un JSON codificado en Base64

### 6.3 Campos del QR

La especificacion oficial del QR publica estos campos:

- `ver`
- `fecha`
- `cuit`
- `ptoVta`
- `tipoCmp`
- `nroCmp`
- `importe`
- `moneda`
- `ctz`
- `tipoDocRec` cuando corresponda
- `nroDocRec` cuando corresponda
- `tipoCodAut`
- `codAut`

Notas importantes:

- `fecha` usa `full-date` RFC3339
- `tipoCodAut` es `"E"` para CAE y `"A"` para CAEA
- el importe se informa en la moneda original del comprobante

### 6.4 Implicancia para el SDK

ARCANet deberia exponer un contrato como:

```csharp
public interface IArcaQrGenerator
{
    ArcaQrPayload BuildPayload(AuthorizedInvoice invoice);
    string BuildJson(ArcaQrPayload payload);
    string BuildBase64(ArcaQrPayload payload);
    Uri BuildUrl(ArcaQrPayload payload);
}
```

El SDK no deberia:

- generar PNG/SVG del QR en el core inicial
- mezclar la especificacion fiscal del QR con librerias graficas

## 7. Representacion visual y datos que deben quedar disponibles

### 7.1 Lo que queda fuera del core inicial

Fuera del core:

- PDF A4
- HTML de factura
- ticket termico
- templates visuales
- impresion
- imagen QR

### 7.2 Lo que el core si debe exponer

Aunque no renderice, el core debe exponer todos los datos necesarios para que una app o paquete futuro genere una representacion valida:

- datos del emisor
- datos del receptor
- tipo y numero de comprobante
- punto de venta
- fecha de emision
- concepto
- moneda y cotizacion
- importes y desglose
- detalle de IVA y tributos informados
- comprobantes asociados
- CAE o tipo de autorizacion
- vencimiento de CAE/CAEA
- observaciones oficiales
- payload/URL del QR

Limite practico:

- cuando el comprobante se obtuvo por `CreateInvoiceAsync`, el SDK si dispone del request original y puede devolver un `AuthorizedInvoice` rico
- cuando el comprobante se reconstruye via `FECompConsultar`, ARCA no siempre devuelve nombre visible del receptor ni la descripcion semantica completa de algunos catalogos
- por eso `GetInvoiceAsync` debe tratarse como helper de verificacion/consulta, no como reemplazo de la persistencia propia de la aplicacion

### 7.3 Diferencia entre web service y comprobante representado

El request tecnico de WSFEv1 no agota el contenido relevante del comprobante final.

Por lo tanto:

- la API publica del SDK no debe reflejar simplemente el request SOAP
- debe existir un modelo de salida orientado a "comprobante autorizado"
- ese modelo debe servir para persistencia, auditoria y representacion

### 7.4 Roadmap de rendering

Se recomienda dejar documentado un paquete futuro:

- `ARCA.Fiscal.Rendering` o `ARCA.Fiscal.Rendering.Pdf`

Posibles responsabilidades futuras:

- PDF A4
- HTML
- ticket termico
- imagen QR
- templates customizables

Ese paquete no debe mezclarse con el core fiscal inicial.

## 8. WSFEv1 vs WSMTXCA

### 8.1 Cuando usar WSFEv1

Usar WSFEv1 cuando:

- se necesita emitir comprobantes sin detalle de item en el servicio fiscal
- se busca menor complejidad inicial
- el sistema propio ya domina lineas, catalogo y calculos internos

### 8.2 Cuando usar WSMTXCA

Usar WSMTXCA cuando:

- el detalle de item es requisito funcional o fiscal relevante
- se necesita trazabilidad de lineas dentro del servicio
- se quiere validar mas semantica a nivel item

### 8.3 Recomendacion MVP

El MVP puede y deberia arrancar con `WSFEv1`.

Que se pierde al no usar WSMTXCA:

- detalle fiscal nativo de items
- algunas validaciones/estructuras especificas por item

Esa perdida es aceptable para un SDK inicial reusable y pragmatico.

## 9. Numeracion, concurrencia e idempotencia

Esta es una de las decisiones mas importantes del proyecto.

### 9.1 Decision de boundary

El SDK no debe ser el duenio final de la numeracion global.

La aplicacion consumidora debe manejar:

- persistencia transaccional
- locking
- reserva o asignacion de numeros
- coordinacion entre cajas, procesos o instancias

### 9.2 Por que `FECompUltimoAutorizado` no alcanza

`FECompUltimoAutorizado` sirve para:

- diagnostico
- bootstrap
- reconciliacion
- verificacion operativa

No alcanza para resolver por si solo:

- dos cajas emitiendo al mismo tiempo
- dos procesos consultando el mismo "ultimo" y tomando el mismo siguiente numero
- duplicacion por timeout y retry

### 9.3 Estrategia recomendada para la aplicacion consumidora

Antes de invocar ARCA, la aplicacion deberia guardar un registro transaccional con al menos:

- `IssuerCuit`
- `PointOfSale`
- `VoucherType`
- `RequestedNumber`
- `ReceiverDocumentType`
- `ReceiverDocumentNumber`
- `IssueDate`
- `Currency`
- `TotalAmount`
- `BusinessIdempotencyKey`
- `Status`
- timestamps de creacion/actualizacion

Estados sugeridos:

- `Draft`
- `NumberReserved`
- `SubmissionInProgress`
- `Authorized`
- `Rejected`
- `UnknownNeedsVerification`
- `CancelledInternally`

### 9.4 Flujo recomendado

1. Abrir transaccion local.
2. Reservar o asignar numero en forma exclusiva para `PtoVta + CbteTipo`.
3. Persistir estado `SubmissionInProgress`.
4. Confirmar transaccion local.
5. Invocar `CreateInvoiceAsync`.
6. Si responde `Authorized`, persistir CAE, vencimiento, QR y estado final.
7. Si responde `Rejected`, persistir codigos y estado final.
8. Si ocurre timeout o error incierto, marcar `UnknownNeedsVerification`.
9. Consultar `FECompConsultar` antes de cualquier reintento.
10. Solo reintentar si la verificacion permite concluir que el comprobante no fue autorizado.

### 9.5 Reintentos

Regla base:

- no hacer retry ciego de `FECAESolicitar`

Tratamiento recomendado:

- error tecnico claramente previo al envio: retry posible
- error tecnico incierto luego de enviar: consultar antes de reintentar
- rechazo funcional: no retry automatico

### 9.6 Dos cajas, dos procesos o dos requests simultaneos

Escenarios reales a contemplar:

- POS 1 y POS 2 intentan emitir sobre mismo punto de venta/tipo
- reintento automatico del balanceador
- timeout HTTP del cliente pero request llego a ARCA
- proceso A queda incierto y proceso B avanza

Por eso:

- la numeracion debe coordinarse fuera del SDK
- el SDK solo puede ayudar, no garantizar unicidad global por si solo

### 9.7 Helpers que el SDK si puede ofrecer

- helper para consultar ultimo autorizado
- helper para consultar comprobante por `PtoVta + CbteTipo + CbteNro`
- helper para clasificar errores tecnicos en `retryable` vs `uncertain`
- helper para construir claves funcionales de idempotencia

### 9.8 Resultado incierto

El SDK deberia tener un resultado funcional explicito para:

- `UnknownStatus`
- `NeedsVerification`

Eso evita forzar a la aplicacion a inferir incertidumbre desde excepciones genricas.

## 10. API publica de alto nivel

### 10.1 Objetivo

El consumidor no deberia pensar en:

- `FECAESolicitar`
- `FECompConsultar`
- `token`
- `sign`
- clases SOAP

Intencion buscada:

```csharp
var result = await client.CreateInvoiceAsync(request, cancellationToken);
```

### 10.2 Interfaces publicas tentativas

```csharp
public interface IArcaClient
{
    IInvoiceClient Invoices { get; }
}

public interface IInvoiceClient
{
    Task<CreateInvoiceResult> CreateInvoiceAsync(
        CreateInvoiceRequest request,
        CancellationToken cancellationToken = default);

    Task<AuthorizedInvoice?> GetInvoiceAsync(
        InvoiceLocator locator,
        CancellationToken cancellationToken = default);

    Task<long?> GetLastAuthorizedNumberAsync(
        VoucherSeries series,
        CancellationToken cancellationToken = default);

    Task<InvoiceValidationResult> ValidateCreateInvoiceAsync(
        CreateInvoiceRequest request,
        CancellationToken cancellationToken = default);
}
```

Interfaces internas o de infraestructura:

- `IWsaaClient`
- `IWsfev1Client`
- `IAccessTicketProvider`
- `ICertificateProvider`
- `IArcaSoapTransport`
- `IClock`
- `IArcaQrGenerator`
- `IInvoiceRequestValidator`

### 10.3 Request publico

```csharp
public sealed class CreateInvoiceRequest
{
    public required long IssuerCuit { get; init; }
    public required VoucherType VoucherType { get; init; }
    public required int PointOfSale { get; init; }
    public required long VoucherNumber { get; init; }
    public required InvoiceConcept Concept { get; init; }
    public required DateOnly IssueDate { get; init; }
    public DateOnly? ServiceFrom { get; init; }
    public DateOnly? ServiceTo { get; init; }
    public DateOnly? PaymentDueDate { get; init; }
    public required CustomerIdentity Customer { get; init; }
    public required ReceiverVatCondition ReceiverVatCondition { get; init; }
    public required MoneyTotals Totals { get; init; }
    public required CurrencyAmount Currency { get; init; }
    public IReadOnlyList<VatItem> VatItems { get; init; } = [];
    public IReadOnlyList<TributeItem> Tributes { get; init; } = [];
    public IReadOnlyList<AssociatedVoucher> AssociatedVouchers { get; init; } = [];
    public string? ExternalIdempotencyKey { get; init; }
}
```

Observaciones:

- el request publico usa nombres de dominio del SDK
- no expone `Auth`, `FeCabReq` ni `FECAEDetRequest`
- incluye de forma explicita los datos que importan a la aplicacion

### 10.4 Respuesta publica

Modelo recomendado:

```csharp
public abstract record CreateInvoiceResult;

public sealed record AuthorizedInvoiceResult(
    AuthorizedInvoice Invoice,
    IReadOnlyList<InvoiceObservation> Observations) : CreateInvoiceResult;

public sealed record RejectedInvoiceResult(
    InvoiceAttempt Attempt,
    IReadOnlyList<InvoiceRejection> Rejections,
    IReadOnlyList<InvoiceObservation> Observations) : CreateInvoiceResult;

public sealed record UnknownInvoiceResult(
    InvoiceAttempt Attempt,
    string Reason,
    bool ShouldQueryBeforeRetry) : CreateInvoiceResult;
```

El comprobante autorizado deberia exponer:

- serie fiscal
- datos del emisor y receptor
- importes
- CAE
- vencimiento del CAE
- fecha de proceso
- tipo de emision
- QR payload
- QR URL
- observaciones

### 10.5 Excepciones tecnicas vs resultados funcionales

Excepciones tecnicas:

- timeout de transporte
- fallo de red
- endpoint caido
- XML invalido inesperado
- certificado no disponible
- firma imposible de producir
- clock skew interno o configuracion imposible

Resultados funcionales:

- comprobante autorizado
- comprobante rechazado
- comprobante autorizado con observaciones
- resultado incierto que requiere consulta

Regla:

- un rechazo de ARCA no deberia lanzarse como excepcion tecnica
- una observacion de ARCA no deberia perderse dentro de logs

### 10.6 Como evitar filtrar SOAP

Reglas de diseno:

- modelos SOAP solo en capa interna `Infrastructure` o `Transport`
- mapeo interno a modelos propios del SDK
- modelo neutral intermedio antes de mapear a SOAP
- los contratos publicos no deben depender de nombres de clases generadas por WSDL
- si en el futuro cambia la forma de consumo SOAP, la API publica no deberia cambiar

### 10.7 Validacion local e internal mapping

Implementado en esta etapa:

- `InvoiceRequestValidator`
- `InvoiceValidationResult`
- `InvoiceValidationError`
- `InternalInvoiceSubmission`
- `IInvoiceSubmissionMapper`

Objetivo:

- validar tecnicamente antes de tocar WSAA/WSFEv1
- fijar un contrato interno estable e independiente del transporte
- reducir el riesgo de acoplar la API publica al futuro mapping SOAP

## 11. Boundaries: SDK vs aplicacion consumidora

### 11.1 Responsabilidades del SDK

- autenticacion WSAA
- emision y consulta WSFEv1
- generacion de payload/JSON/Base64/URL del QR fiscal
- validaciones tecnicas basicas
- mapeo de errores, rechazos y observaciones oficiales
- abstracciones de transporte, certificado y reloj
- helpers de numeracion/consulta/idempotencia

### 11.2 Responsabilidades de la aplicacion consumidora

- persistencia transaccional
- locking y coordinacion concurrente
- numeracion propia y reserva de numeros
- UI
- PDF/impresion/rendering inicial
- reglas comerciales
- decision fiscal final con contador/profesional
- almacenamiento seguro de secretos/certificados segun infraestructura
- auditoria de negocio

## 12. Manejo de errores

### 12.1 Tipos

Errores tecnicos:

- red
- timeout
- TLS
- SOAP malformado
- credenciales WSAA vencidas o invalidas
- certificado vencido/ausente

Rechazos:

- no se autoriza el comprobante
- pueden incluir codigos y mensajes oficiales

Observaciones:

- puede haber autorizacion con observaciones
- esas observaciones deben llegar al consumidor

Resultado incierto:

- el SDK no sabe si ARCA autorizo o no
- se requiere consulta antes de repetir

### 12.2 Validaciones previas del SDK

El SDK deberia validar antes de invocar:

- campos requeridos
- consistencia de importes
- compatibilidad de receptor con clase de comprobante
- presencia de comprobantes asociados para notas de credito cuando el escenario lo requiera
- moneda/cotizacion
- fechas segun concepto
- validez basica de numeros y documentos

Limite:

- no replicar toda la normativa fiscal como si fuera una verdad absoluta y cerrada

## 13. Testing strategy

### 13.1 Unit tests

- mapeo de requests del dominio a modelos internos
- mapeo de responses oficiales a resultados publicos
- clasificacion de errores/rechazos/observaciones
- validaciones previas
- QR payload
- QR JSON/Base64/URL

### 13.2 Fixtures y golden files

Conviene usar:

- fixtures XML de WSAA
- fixtures XML de WSFEv1
- golden files de request/response
- golden files del QR payload JSON

### 13.3 Test doubles

- mock/fake de `IArcaSoapTransport`
- fake de `IClock`
- fake de `ICertificateProvider`
- fake de `IAccessTicketProvider`

### 13.4 Integration tests

- integration tests contra homologacion
- desactivados por defecto
- solo habilitados con variables de entorno y secretos locales
- preferentemente separados de unit tests por `Trait/Category=Integration`
- para una primera etapa conviene empezar con smoke tests sin efectos fiscales:
  - login WSAA para `wsfe`
  - consulta de ultimo autorizado
  - consulta de comprobante existente si la app provee un numero ya emitido
- no conviene automatizar de entrada tests que emitan nuevos comprobantes en cada corrida

Variables sugeridas:

- `ARCANET_RUN_HOMOLOGATION_TESTS`
- `ARCANET_TEST_ENVIRONMENT`
- `ARCANET_TEST_CUIT`
- `ARCANET_TEST_CERTIFICATE_PATH`
- `ARCANET_TEST_CERTIFICATE_PASSWORD`
- `ARCANET_TEST_POINT_OF_SALE`

Reglas:

- nunca commitear certificados
- nunca commitear tokens
- nunca commitear secretos
- nunca commitear CUIT reales de terceros

## 14. Diseño tecnico inicial

### 14.1 Contratos principales

```csharp
public interface IWsaaClient { }
public interface IWsfev1Client { }
public interface IAccessTicketProvider { }
public interface ICertificateProvider { }
public interface IArcaSoapTransport { }
public interface IClock { }
public interface IArcaQrGenerator { }
```

### 14.2 Options por ambiente

```csharp
public enum ArcaEnvironment
{
    Homologation,
    Production
}
```

Options sugeridas:

- `ArcaOptions`
- `WsaaOptions`
- `Wsfev1Options`
- `QrOptions`

### 14.3 Estructura tentativamente recomendada

```text
ARCANet/
  Abstractions/
  Configuration/
  Common/
    Results/
    Errors/
    ValueObjects/
  Auth/
    Wsaa/
  Billing/
    Invoices/
    Wsfev1/
    Qr/
  Infrastructure/
    Soap/
    Security/
    Serialization/
    Diagnostics/
  DependencyInjection/
```

### 14.4 ASP.NET Core futuro

Conviene dejar preparado un paquete o namespace de DI:

```csharp
services.AddArcaNet(options =>
{
    options.Environment = ArcaEnvironment.Homologation;
});
```

Sin embargo:

- no hace falta implementarlo todavia
- la API de configuracion deberia pensarse desde ahora

## 15. Que queda fuera del MVP

Fuera del MVP inicial:

- implementacion SOAP real
- proxies WSDL
- firma CMS real
- soporte WSMTXCA
- soporte completo de todos los comprobantes
- CAEA
- rendering PDF/HTML/ticket
- imagen QR
- almacenamiento distribuido de access tickets
- reglas fiscales exhaustivas por industria/regimen especial
- automatizacion de tramites administrativos

Importante:

- el QR fiscal no queda fuera; solo queda fuera la imagen/render del QR

## 16. Riesgos tecnicos y fiscales

Riesgos tecnicos:

- mala gestion de vigencia `token/sign`
- drift horario
- retries ciegos que dupliquen facturacion
- mezcla de dominio con WSDL
- hardcode de ambiente o endpoints

Riesgos fiscales:

- asumir que "si paso WSFEv1 entonces el comprobante ya esta fiscalmente bien"
- no contemplar condicion IVA del receptor
- anular mal una operacion con notas de credito
- omitir QR, CAE o vencimiento en la representacion

Mitigacion:

- separar validacion tecnica de validacion fiscal
- marcar pendientes donde haga falta criterio profesional
- exponer todos los datos relevantes para posterior representacion/auditoria

## 17. Plan por etapas

Etapa 0:

- definir contratos publicos
- definir modelos del dominio publico
- definir boundaries

Etapa 1:

- WSAA interno
- parser de access ticket
- cache basica de credenciales

Etapa 2:

- `CreateInvoiceAsync`
- `GetInvoiceAsync`
- `GetLastAuthorizedNumberAsync`
- QR payload/URL

Etapa 3:

- robustez de errores
- helper de verificacion ante estados inciertos
- tablas parametricas y cache

Etapa 4:

- package de rendering opcional
- mas comprobantes
- evaluacion de WSMTXCA

## 18. Lista de verificacion de cumplimiento fiscal/tecnico para el MVP

Resumen ejecutivo del checklist:

- confirmado por documentacion oficial:
  - WSAA con `token/sign`
  - WSFEv1 con autorizacion/consulta
  - QR fiscal obligatorio
  - CAE y vencimiento
  - datos obligatorios del comprobante segun RG `1415/2003`
  - notas de credito asociadas a comprobantes previos
  - condicion IVA del receptor como dato relevante del flujo actual
- pendiente de validacion fiscal/contable:
  - reglas completas por tipo de receptor
  - alcance exacto de consumidor final en casuisticas especiales
  - criterios completos para `C` y `M`
  - leyendas/campos visuales adicionales exigibles segun normativa aplicable
- fuera del MVP pero relevante:
  - `CAEA`
  - rendering
  - imagen QR
  - regimens especiales
  - `WSMTXCA`

Detalle del checklist:

- [compliance-checklist.md](compliance-checklist.md)

## Conclusiones

La recomendacion sigue siendo construir ARCANet sobre `WSAA + WSFEv1`, pero corrigiendo el enfoque: el valor del proyecto no debe ser "envolver SOAP", sino ofrecer una API fiscal/tecnica de alto nivel que reduzca errores reales de integracion.

Eso implica:

- abstraer autenticacion y transporte
- modelar resultados funcionales claros
- tratar la numeracion y la incertidumbre como problemas de primera clase
- incluir QR fiscal desde el MVP
- exponer todos los datos necesarios para representar comprobantes validos, aunque el rendering quede para un paquete futuro

Los puntos que requieren criterio profesional deben mantenerse marcados como `pendiente de validacion fiscal/contable`.
