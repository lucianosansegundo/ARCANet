# ARCANet.Persistence.Postgres

Optional PostgreSQL persistence module for `ARCANet`.

This package provides:

- `PostgresAccessTicketStore`
- `PostgresAccessTicketStoreOptions`
- durable WSAA access ticket storage
- advisory-lock coordination per ticket key

Typical use:

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

Recommended usage:

- single-process/local: consider the core `InMemoryAccessTicketStore` or `FileAccessTicketStore`
- production or multi-instance: use `PostgresAccessTicketStore`

Important boundary:

- this package persists WSAA access tickets
- it does not solve invoice numbering or business idempotency for the consuming POS/application
