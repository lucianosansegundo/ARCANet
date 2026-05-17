# ARCANet.Persistence.Postgres

Modulo opcional de persistencia PostgreSQL para `ARCANet`.

Este paquete ofrece:

- `PostgresAccessTicketStore`
- `PostgresAccessTicketStoreOptions`
- persistencia durable de access tickets WSAA
- coordinacion por key usando advisory locks

Uso tipico:

```csharp
using ARCANet.Persistence.Postgres;

await using var store = await PostgresAccessTicketStore.CreateInitializedAsync(
    connectionString,
    new PostgresAccessTicketStoreOptions
    {
        SchemaName = "public",
        TableName = "arca_access_tickets"
    });
```

Uso recomendado:

- proceso simple o local: considerar `InMemoryAccessTicketStore` o `FileAccessTicketStore`
- produccion o multi-instancia: usar `PostgresAccessTicketStore`

Limite importante:

- este paquete persiste access tickets WSAA
- no resuelve numeracion de comprobantes ni idempotencia de negocio del POS o de la aplicacion consumidora
