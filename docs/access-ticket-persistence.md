# Persistencia de Access Tickets

Este documento define la estrategia recomendada para persistencia y reutilizacion de tickets de acceso (`TA`) de `WSAA` en `ARCANet`.

## Contexto

En pruebas reales de homologacion se observo este comportamiento de `WSAA`:

- un `loginCms` exitoso entrega un `TA` valido para `wsfe`
- mientras ese `TA` siga vigente, `WSAA` puede rechazar nuevos pedidos con faults como:
  - `coe.alreadyAuthenticated`
  - `El CEE ya posee un TA valido para el acceso al WSN solicitado`

Esto implica que perder localmente el `TA` luego de un login exitoso puede convertirse en un problema operativo real.

## Problema

El cache actual en memoria alcanza solo para:

- una sola instancia
- un solo proceso vivo
- una sola corrida continua

No alcanza para:

- reinicios del proceso
- herramientas temporales o jobs separados
- multiples instancias
- entornos serverless
- diagnosticos repetidos en homologacion

## Objetivo

`ARCANet` debe permitir:

- reutilizar `TA`s vigentes sin volver a llamar `loginCms`
- persistir `TA`s entre procesos si la aplicacion lo necesita
- evitar acoplar el core a filesystem, base de datos o un proveedor especifico
- mantener el diseño reusable y portable

## Decision de arquitectura

La libreria no debe asumir una unica estrategia de persistencia.

La estrategia correcta es:

- el core define contratos y flujo
- la aplicacion consumidora elige el backend de persistencia
- `ARCANet` ofrece implementaciones listas para usar para casos comunes

## Contratos implementados

```csharp
public interface IAccessTicketStore
{
    Task<StoredAccessTicket?> GetAsync(
        AccessTicketStoreKey key,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        AccessTicketStoreKey key,
        StoredAccessTicket ticket,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        AccessTicketStoreKey key,
        CancellationToken cancellationToken = default);
}

public sealed record AccessTicketStoreKey(
    ArcaEnvironment Environment,
    string Service,
    long RepresentedCuit,
    string CertificateIdentifier);

public sealed record StoredAccessTicket(
    string Token,
    string Sign,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset StoredAtUtc);
```

## Keying strategy

La key no debe ser solo `wsfe`.

Debe discriminar como minimo:

- ambiente
- servicio
- CUIT representado
- identificador del certificado o del CEE

Ejemplo conceptual:

```text
homologation:wsfe:20287812265:thumbprint-abc123
```

Si no se discrimina por esos factores, es facil mezclar tickets incompatibles.

## Implementaciones disponibles

### En el core

- `InMemoryAccessTicketStore`
- `NullAccessTicketStore`

`InMemoryAccessTicketStore` es el default actual si el consumidor no configura nada.

### Modulo liviano opcional

- `FileAccessTicketStore`

Caso de uso ideal:

- homologacion local
- consola
- desktop
- debugging

Estado actual:

- implementado en el package actual como store opcional basado en filesystem local
- pensado para homologacion local, consola y escenarios de diagnostico
- serializa acceso por archivo dentro del mismo proceso para evitar lecturas/escrituras pisadas
- expone coordinacion por key para que el provider pueda evitar `loginCms` duplicados entre procesos locales que compartan el mismo directorio
- no agrega locking distribuido entre maquinas distintas ni coordinacion multi-instancia avanzada

### Roadmap opcional

- `ARCA.Fiscal.Persistence.Redis`
- `ARCA.Fiscal.Persistence.EntityFramework`
- `ARCA.Fiscal.Persistence.SqlServer`

### Store recomendado para ECS/RDS

Si la app corre en `ECS/Fargate` y ya usa `PostgreSQL` en `RDS`, el store recomendado para produccion es:

- `PostgresAccessTicketStore`
- idealmente desde un package/modulo opcional como `ARCA.Fiscal.Persistence.Postgres`

API recomendada del modulo PostgreSQL:

- `PostgresAccessTicketStore`
- `PostgresAccessTicketStoreOptions`

Notas de diseno:

- `schema` y `table` se configuran por opciones tipadas
- los identificadores PostgreSQL se validan y quoted automaticamente
- el store expone `EnsureTableExistsAsync` para inicializacion explicita
- el store expone `CreateInitializedAsync(...)` para crear + asegurar tabla en una sola llamada
- el SQL de bootstrap tambien puede obtenerse con `BuildCreateTableSql(...)`

Motivo:

- el filesystem local de `Fargate` no sirve como coordinacion/shared cache entre tasks
- `RDS` ya existe en la arquitectura
- PostgreSQL permite `upsert` atomico y `advisory locks` por key
- el mismo backend de persistencia sirve para uno o muchos tasks del POS/API

Estas implementaciones no deberian forzar dependencias de infraestructura en el core.

## Uso de PostgreSQL

Si una aplicacion necesita persistencia durable y coordinacion entre multiples instancias, la opcion recomendada es `ARCA.Fiscal.Persistence.Postgres`.

Escenario tipico:

- backend o POS desplegado en mas de una instancia
- reinicios posibles del proceso
- necesidad de evitar `loginCms` duplicados para la misma combinacion de ambiente/servicio/CUIT/certificado
- PostgreSQL ya disponible en la app

### 1. Instalar el modulo opcional

El consumidor instala:

- `ARCA.Fiscal`
- `ARCA.Fiscal.Persistence.Postgres`

### 2. Elegir schema y tabla

Por default:

- `schema`: `public`
- `table`: `arca_access_tickets`

Si hace falta, pueden configurarse con `PostgresAccessTicketStoreOptions`.

### 3. Crear e inicializar el store

Forma mas simple:

```csharp
using ARCANet.Persistence.Postgres;

await using var ticketStore = await PostgresAccessTicketStore.CreateInitializedAsync(
    connectionString,
    new PostgresAccessTicketStoreOptions
    {
        SchemaName = "public",
        TableName = "arca_access_tickets"
    },
    cancellationToken);
```

Si la app prefiere manejar bootstrap por separado:

```csharp
using ARCANet.Persistence.Postgres;

await using var ticketStore = new PostgresAccessTicketStore(
    connectionString,
    new PostgresAccessTicketStoreOptions
    {
        SchemaName = "public",
        TableName = "arca_access_tickets"
    });

await ticketStore.EnsureTableExistsAsync(cancellationToken);
```

Si la app no quiere auto-crear la tabla en runtime, puede tomar el SQL y ejecutarlo desde su pipeline/migracion:

```csharp
var sql = PostgresAccessTicketStore.BuildCreateTableSql(
    new PostgresAccessTicketStoreOptions
    {
        SchemaName = "public",
        TableName = "arca_access_tickets"
    });
```

### 4. Pasarlo al provider WSAA

```csharp
using ARCANet.Wsaa;

var accessTicketProvider = new WsaaAccessTicketProvider(
    certificateProvider,
    transport,
    clock,
    wsaaOptions,
    ticketStore);
```

### 5. Que obtiene la app con este store

- persistencia durable del `TA`
- `upsert` atomico por key
- coordinacion por key en PostgreSQL usando `pg_advisory_xact_lock`
- reuso de tickets entre multiples instancias de la aplicacion usando la misma base

### 6. Recomendacion operativa

Para una libreria OSS, la mejor UX para el consumidor suele ser:

- `InMemoryAccessTicketStore` para casos simples o pruebas
- `FileAccessTicketStore` para homologacion/local
- `PostgresAccessTicketStore` para produccion y multi-instancia

### 7. Sobre tests de integracion del modulo PostgreSQL

Los tests reales contra PostgreSQL son para mantenimiento y validacion del proyecto, no para el consumidor final de la libreria.

El usuario que integra `ARCA.Fiscal.Persistence.Postgres` no deberia necesitar correr esos tests para usar el store en su app.

Estado actual:

- la suite usa `Testcontainers`
- requiere Docker solo para correr esos tests de integracion
- se habilita con `ARCANET_RUN_POSTGRES_INTEGRATION_TESTS=true`

## Flujo implementado del provider

`WsaaAccessTicketProvider`:

1. Construir una `AccessTicketStoreKey`.
2. Consultar `IAccessTicketStore`.
3. Si existe un ticket vigente con margen suficiente, devolverlo.
4. Si no existe o esta vencido, ejecutar `loginCms`.
5. Persistir el nuevo ticket en el store.
6. Devolver el ticket persistido.

Detalles implementados en Phase 1:

- la key incluye:
  - `Environment`
  - `Service`
  - `RepresentedCuit`
  - `CertificateIdentifier`
- `RepresentedCuit` se deriva del `serialNumber` del subject del certificado con formato `CUIT 20123456789`
- `CertificateIdentifier` usa el `Thumbprint` del certificado
- la reutilizacion usa el margen `RefreshBeforeExpiration` para decidir si un ticket sigue siendo utilizable
- requests concurrentes para la misma key comparten un lock por key dentro del proceso para evitar `loginCms` duplicados
- si el store implementa coordinacion por key, el provider extiende ese lock a todo el bloque `read -> login -> save`

## Manejo de vigencia

La vigencia debe evaluarse con margen defensivo:

- no esperar al segundo exacto de vencimiento
- renovar antes usando `RefreshBeforeExpiration`

## Manejo de alreadyAuthenticated

Si `WSAA` responde `coe.alreadyAuthenticated` y el SDK no tiene un `TA` recuperable desde el store:

- no debe hacer retry ciego
- debe devolver un error tecnico claro
- debe indicar que existe un `TA` remoto vigente no disponible localmente

Esto no debe mapearse como rechazo funcional de negocio.

## Boundaries

Responsabilidad del SDK:

- definir el flujo de recuperacion/renovacion
- ofrecer defaults seguros
- ofrecer implementaciones base listas para usar
- exponer errores tecnicos claros

Responsabilidad de la app consumidora:

- decidir si alcanza memoria o necesita persistencia durable
- elegir backend de store compatible con su infraestructura
- resolver multi-instancia si aplica
- proteger el almacenamiento del `TA` segun su modelo de seguridad

## Sobre filesystem

El filesystem no debe ser obligatorio.

Motivo:

- no siempre existe
- no siempre es durable
- no siempre es compartido
- no siempre esta permitido

Por eso `FileAccessTicketStore` sirve para homologacion y desarrollo local, pero no debe ser la unica ni la principal estrategia del core.

## Estado actual

Estado actual de `ARCANet`:

- store abstraido con `IAccessTicketStore`
- `InMemoryAccessTicketStore` como default seguro
- `NullAccessTicketStore` disponible para desactivar persistencia/reuso local
- `WsaaAccessTicketProvider` integrado con store, reuse y refresh defensivo
- persistencia durable entre procesos todavia delegada a implementaciones futuras o externas

## Fases recomendadas

### Phase 1

- Agregar contratos:
  - `IAccessTicketStore`
  - `AccessTicketStoreKey`
  - `StoredAccessTicket`
- Agregar implementaciones base:
  - `InMemoryAccessTicketStore`
  - `NullAccessTicketStore`
- Integrar store en `WsaaAccessTicketProvider`
- Agregar tests unitarios de reuso, renovacion y keys

Estado:

- implementado
- cubierto con tests de:
  - fresh stored ticket evita `loginCms`
  - ticket vencido o dentro del margen dispara `loginCms`
  - ticket nuevo se persiste
  - keying por ambiente/servicio/CUIT/certificado
  - `alreadyAuthenticated` sin ticket util recuperable produce error tecnico claro

### Phase 2

- Agregar `FileAccessTicketStore`
- Orientarlo a:
  - homologacion local
  - consola
  - desktop
  - debugging
- Mantenerlo fuera del core si agrega friccion o dependencia innecesaria

Estado:

- implementado con persistencia JSON por key
- cubierto con tests de round-trip, delete, reuso entre providers distintos compartiendo el mismo directorio y escrituras concurrentes locales legibles
- coordinacion por key implementada en `FileAccessTicketStore` con mutex nombrado del sistema para procesos locales separados

### Phase 3

- Evaluar stores opcionales para infraestructura real:
  - Redis
  - SQL
  - Entity Framework
- Evaluar locking distribuido si aparece necesidad multi-instancia

Estado del problema de concurrencia hoy:

- resuelto dentro del mismo proceso para una misma key de access ticket
- resuelto a nivel de acceso local al archivo dentro del mismo proceso
- resuelto para procesos locales distintos que compartan `FileAccessTicketStore` sobre el mismo directorio
- resuelto para multiples instancias usando `PostgresAccessTicketStore` sobre la misma base PostgreSQL
- pendiente para otros stores remotos sin locking distribuido

## Roadmap recomendado

1. Agregar contratos:
   - `IAccessTicketStore`
   - `AccessTicketStoreKey`
   - `StoredAccessTicket`
2. Agregar implementaciones base:
   - `InMemoryAccessTicketStore`
   - `NullAccessTicketStore`
3. Integrar store en `WsaaAccessTicketProvider`.
4. Agregar tests unitarios de reuso, renovacion y keys.
5. Agregar `FileAccessTicketStore` como implementacion orientada a homologacion/local.
6. Documentar opciones futuras para Redis/SQL/EF.

## Conclusion

La persistencia de tickets de acceso no es un detalle opcional de conveniencia. Es parte del diseño operativo correcto de una integracion con `WSAA`.

La libreria debe:

- ofrecer implementaciones usables
- permitir configuracion explicita
- no asumir filesystem
- seguir siendo generica y reusable
