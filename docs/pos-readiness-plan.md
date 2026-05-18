# Plan de Readiness para POS

Este documento traduce el estado actual de `ARCANet` a un roadmap concreto para usar la libreria como base de un `POS` real que facture con ARCA/AFIP.

Objetivo:

- identificar que ya existe
- identificar que falta para un `POS` que emita comprobantes reales
- definir un orden de implementacion pragmatico
- separar lo tecnico de infraestructura de lo funcional/fiscal/operativo

Importante:

- este plan no reemplaza validacion contable/fiscal profesional
- "listo para POS" no significa automaticamente "listo para cualquier regimen especial"
- la prioridad inicial es dejar robusto el camino de:
  - Factura A
  - Factura B
  - Nota de Credito A
  - Nota de Credito B

## 1. Resumen ejecutivo

Estado real hoy:

- la base de autenticacion WSAA ya esta razonablemente encaminada
- la persistencia de tickets de acceso ya tiene opciones viables para local y para multi-instancia con PostgreSQL
- la emision WSFEv1 base existe
- la validacion local existe
- la consulta de comprobantes y de ultimo autorizado existe
- el QR fiscal existe

Lo que todavia falta para un `POS` usable con confianza:

- homologacion funcional end-to-end repetible para emision real
- flujo de nota de credito como operacion de negocio clara
- estrategia operativa de numeracion e idempotencia alrededor del SDK
- cierre de manejo de estados inciertos
- definicion del conjunto minimo de escenarios POS soportados
- endurecimiento de documentacion/ergonomia para integradores

## 2. Matriz de capacidades

### Ya soportado o bastante encaminado

- obtencion de `TA` WSAA
- reuse/refresh/persistencia de `TA`
- store PostgreSQL para multi-instancia
- construccion de request WSFEv1
- `CreateInvoiceAsync`
- `GetInvoiceAsync`
- `GetLastAuthorizedNumberAsync`
- modelado de `Factura A`
- modelado de `Factura B`
- modelado de `Nota de Credito A`
- modelado de `Nota de Credito B`
- asociacion de comprobantes
- condicion IVA del receptor en request
- QR fiscal
- resultado autorizado / rechazado / incierto

### Parcial o con madurez insuficiente para POS

- homologacion real de emision
- reglas operativas de anulacion por nota de credito
- recuperacion ante timeout o estado incierto
- ergonomia de integracion del modulo PostgreSQL
- contratos/documentacion de wiring para consumidores
- cobertura de integration tests reales contra PostgreSQL
- coverage de smoke tests de homologacion limitada a lectura

### Faltante real para un POS mas confiable

- suite opt-in de homologacion que emita comprobantes reales controlados
- helper o servicio de verificacion/reconciliacion ante estados inciertos
- politica clara de idempotencia funcional
- guia de implementacion de numeracion para apps consumidoras
- operacion de dominio explicita para notas de credito
- documentacion de escenarios POS tipicos
- mayor claridad sobre regimenes fuera del alcance inicial

## 3. Escenarios POS que deberian quedar cubiertos primero

### Prioridad alta

- emitir `Factura B` a consumidor final
- emitir `Factura B` a receptor identificado
- emitir `Factura A` a responsable inscripto
- emitir `Nota de Credito B` asociada a comprobante previo
- emitir `Nota de Credito A` asociada a comprobante previo
- consultar comprobante emitido
- obtener ultimo autorizado por `PtoVta + Tipo`

### Prioridad media

- manejo de errores tecnicos con verificacion posterior
- reimpresion/reconstruccion parcial de comprobante emitido
- anulacion total de operacion via nota de credito
- devolucion parcial via nota de credito

### Fuera del primer cierre POS

- Factura C
- Factura M
- Nota de Debito
- CAEA
- regimens especiales
- WSMTXCA

## 4. Gaps concretos a cerrar

### 4.1 Homologacion funcional real

Hoy:

- los tests de homologacion son smoke tests de lectura
- no emiten comprobantes nuevos

Falta:

- validar `CreateInvoiceAsync` real en homologacion
- validar `Factura A/B`
- validar `Nota de Credito A/B`

Riesgo si no se hace:

- el flujo existe en codigo pero no queda suficientemente probado frente a AFIP/ARCA real

### 4.2 Operacion de anulacion

Hoy:

- la anulacion se expresa implicitamente como nota de credito
- el SDK valida comprobantes asociados

Implementado ahora:

- `CreditNoteRequestFactory` para derivar notas de credito A/B desde una factura autorizada original
- camino explicito para cancelacion total y ajuste parcial

Falta:

- documentar mas ejemplos operativos de anulacion total/parcial
- evaluar si en el futuro conviene un helper mas orientado a casos de negocio

Riesgo si no se hace:

- cada integrador puede modelarlo distinto y cometer errores operativos

### 4.3 Estados inciertos

Hoy:

- `CreateInvoiceAsync` devuelve `UnknownInvoiceResult` ante fallos tecnicos

Implementado ahora:

- `InvoiceSubmissionRecovery` para consultar `FECompConsultar` a partir de `UnknownInvoiceResult` o `InvoiceAttempt`

Falta:

- documentacion operativa exacta del flujo recomendado
- politica concreta de retry para apps consumidoras

Riesgo si no se hace:

- reintentos incorrectos
- duplicacion de emision

### 4.4 Numeracion e idempotencia

Hoy:

- el SDK deja claro que la numeracion no debe ser propiedad del core
- existen docs de boundary

Falta:

- guia de implementacion concreta para apps POS
- recomendaciones minimas de lock/transaccion
- helper opcional de claves funcionales si aporta valor real

Riesgo si no se hace:

- integraciones consumidoras resuelven mal el problema mas delicado del POS

### 4.5 Ergonomia de integracion

Hoy:

- existen contratos y stores
- PostgreSQL ya tiene modulo opcional oficial

Falta:

- ejemplos completos mas orientados a consumidor
- eventualmente helpers de DI/configuracion

Riesgo si no se hace:

- mas friccion para adopcion

## 5. Plan por fases

### Fase 1. Cierre de homologacion funcional basica

Objetivo:

- probar de punta a punta el flujo real minimo para el POS inicial

Entregables:

- integration tests opt-in que emitan `Factura B`
- integration tests opt-in que emitan `Factura A`
- integration tests opt-in que emitan `Nota de Credito B`
- integration tests opt-in que emitan `Nota de Credito A`
- documentacion de setup para esta suite

Condicion de salida:

- emision real verificada en homologacion para esos cuatro casos

Estado actual:

- `Factura B` ya fue validada end-to-end en homologacion
- `Factura A` ya fue validada end-to-end en homologacion
- `Nota de Credito B` ya fue validada end-to-end en homologacion
- `Nota de Credito A` ya fue validada end-to-end en homologacion

### Fase 2. Operacion segura ante estados inciertos

Objetivo:

- evitar reintentos incorrectos y mejorar la historia operativa del POS

Entregables:

- helper o servicio de verificacion post-error
- documentacion exacta de:
  - timeout
  - fallo tecnico
  - consulta posterior
  - criterio antes de reintentar
- tests unitarios del flujo de clasificacion/verificacion

Condicion de salida:

- camino documentado y verificable para no duplicar emision

Estado actual:

- helper base implementado
- guia operativa base agregada
- faltan recomendaciones mas explicitas si el proyecto quiere automatizar politicas de retry mas opinionadas

### Fase 3. Ergonomia de nota de credito/anulacion

Objetivo:

- hacer mas claro y menos riesgoso el uso de notas de credito desde un POS

Entregables:

- guia de uso explicita para anulacion total y parcial
- evaluacion de helper/operacion especifica para nota de credito
- ejemplos completos de request asociado

Condicion de salida:

- el integrador no tiene que inferir solo como anular correctamente en el alcance soportado

Estado actual:

- helper base implementado
- guia operativa agregada en `docs/credit-note-usage.md`
- faltan ejemplos mas profundos si el proyecto quiere cubrir casos de negocio mas complejos

### Fase 4. Guia operativa de numeracion e idempotencia

Objetivo:

- bajar el riesgo mas serio fuera del transporte

Entregables:

- documento de integracion POS sobre:
  - reserva de numero
  - lock por `PtoVta + Tipo`
  - persistencia de intento
  - estados del intento
  - reconciliacion posterior
- opcional: helper de clave de idempotencia funcional si el diseño lo justifica

Condicion de salida:

- la libreria sigue sin aduenarse de la numeracion, pero el consumidor ya no queda sin guia

Estado actual:

- guia operativa inicial agregada en `docs/pos-numbering-and-recovery.md`
- no se agregaron helpers de numeracion, a proposito

### Fase 5. Pulido de experiencia de integracion

Objetivo:

- hacer mas facil adoptar la libreria en una app real

Entregables:

- mas ejemplos de wiring
- helpers de configuracion/DI si valen la pena
- docs de seleccion de store por escenario
- documentacion XML de la API publica antes de `1.0`

Condicion de salida:

- el consumo deja de sentirse "de bajo nivel"

Nota:

- `CS1591` esta silenciado temporalmente en los paquetes para evitar ruido en CI mientras la API sigue moviendose
- antes de una `1.0.0`, la superficie publica debe quedar documentada con comentarios XML utiles y no solo con silenciamiento del warning

## 6. Orden recomendado de trabajo

Orden pragmatico:

1. homologacion funcional real de `Factura A/B`
2. homologacion funcional real de `Nota de Credito A/B`
3. flujo seguro de estado incierto
4. documentacion operativa de numeracion e idempotencia
5. mejoras de ergonomia

Motivo:

- antes de agregar mucha API nueva, conviene verificar el flujo real contra ARCA
- una vez verificado, ya vale la pena encapsular mejor los escenarios POS

## 7. Criterio de "listo para usar como base de POS"

Se puede considerar que `ARCANet` esta lista para usarse como base de un POS inicial cuando se cumpla todo esto:

- `Factura A` homologada end-to-end
- `Factura B` homologada end-to-end
- `Nota de Credito A` homologada end-to-end
- `Nota de Credito B` homologada end-to-end
- guia operativa clara de numeracion
- guia clara para estados inciertos
- store productivo durable documentado y probado
- docs de consumo suficientes para integrar sin leer todo el diseño interno

## 8. No objetivos de este plan

Este plan no intenta cubrir todavia:

- todos los comprobantes posibles
- todos los regimenes especiales
- rendering PDF/impresion
- cajon fiscal o hardware POS
- sincronizacion offline compleja
- CAEA o contingencias avanzadas

## 9. Recomendacion inmediata

Siguiente paso recomendado para el proyecto:

- cerrar mejor la ergonomia de anulacion y nota de credito
- despues evaluar helpers de configuracion/integracion

Siguiente paso recomendado para un integrador POS real:

- usar ya `PostgresAccessTicketStore`
- asumir que la numeracion/idempotencia vive en la app
- seguir la guia de `docs/pos-numbering-and-recovery.md`
- no salir a produccion sin un flujo propio de reserva de numero, persistencia de intento y reconciliacion posterior
