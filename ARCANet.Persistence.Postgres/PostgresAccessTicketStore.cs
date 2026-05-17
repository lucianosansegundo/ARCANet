using System.Data;
using System.Security.Cryptography;
using System.Text;
using ARCANet.Abstractions;
using ARCANet.Authentication;
using Npgsql;

namespace ARCANet.Persistence.Postgres;

public sealed class PostgresAccessTicketStore : IAccessTicketStore, IAccessTicketStoreSynchronization, IAsyncDisposable
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly string _qualifiedTableName;

    public PostgresAccessTicketStore(
        string connectionString,
        PostgresAccessTicketStoreOptions? options = null)
        : this(CreateDataSource(connectionString), ownsDataSource: true, options)
    {
    }

    public PostgresAccessTicketStore(
        NpgsqlDataSource dataSource,
        PostgresAccessTicketStoreOptions? options = null)
        : this(dataSource, ownsDataSource: false, options)
    {
    }

    private PostgresAccessTicketStore(
        NpgsqlDataSource dataSource,
        bool ownsDataSource,
        PostgresAccessTicketStoreOptions? options)
    {
        _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
        OwnsDataSource = ownsDataSource;
        Options = options ?? new PostgresAccessTicketStoreOptions();
        _qualifiedTableName = Options.GetQualifiedTableName();
    }

    internal bool OwnsDataSource { get; }

    public PostgresAccessTicketStoreOptions Options { get; }

    public async Task<StoredAccessTicket?> GetAsync(
        AccessTicketStoreKey key,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText =
            $"""
            select token, sign, expires_at_utc, stored_at_utc
            from {_qualifiedTableName}
            where environment = @environment
              and service = @service
              and represented_cuit = @represented_cuit
              and certificate_identifier = @certificate_identifier;
            """;
        AddKeyParameters(command, key);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        return new StoredAccessTicket(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetFieldValue<DateTimeOffset>(2),
            reader.GetFieldValue<DateTimeOffset>(3));
    }

    public async Task SaveAsync(
        AccessTicketStoreKey key,
        StoredAccessTicket ticket,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(ticket);

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText =
            $"""
            insert into {_qualifiedTableName} (
                environment,
                service,
                represented_cuit,
                certificate_identifier,
                token,
                sign,
                expires_at_utc,
                stored_at_utc)
            values (
                @environment,
                @service,
                @represented_cuit,
                @certificate_identifier,
                @token,
                @sign,
                @expires_at_utc,
                @stored_at_utc)
            on conflict (environment, service, represented_cuit, certificate_identifier)
            do update set
                token = excluded.token,
                sign = excluded.sign,
                expires_at_utc = excluded.expires_at_utc,
                stored_at_utc = excluded.stored_at_utc;
            """;
        AddKeyParameters(command, key);
        command.Parameters.AddWithValue("token", ticket.Token);
        command.Parameters.AddWithValue("sign", ticket.Sign);
        command.Parameters.AddWithValue("expires_at_utc", ticket.ExpiresAtUtc.UtcDateTime);
        command.Parameters.AddWithValue("stored_at_utc", ticket.StoredAtUtc.UtcDateTime);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(
        AccessTicketStoreKey key,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText =
            $"""
            delete from {_qualifiedTableName}
            where environment = @environment
              and service = @service
              and represented_cuit = @represented_cuit
              and certificate_identifier = @certificate_identifier;
            """;
        AddKeyParameters(command, key);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<T> ExecuteSerializedAsync<T>(
        AccessTicketStoreKey key,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(action);

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken).ConfigureAwait(false);

        await using (var lockCommand = connection.CreateCommand())
        {
            lockCommand.Transaction = transaction;
            lockCommand.CommandText = "select pg_advisory_xact_lock(@lock_key);";
            lockCommand.Parameters.AddWithValue("lock_key", GetAdvisoryLockKey(key));
            await lockCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        var result = await action(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return result;
    }

    public async Task EnsureTableExistsAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = BuildCreateTableSql(Options);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        if (OwnsDataSource)
        {
            await _dataSource.DisposeAsync().ConfigureAwait(false);
        }
    }

    public static async Task<PostgresAccessTicketStore> CreateInitializedAsync(
        string connectionString,
        PostgresAccessTicketStoreOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var store = new PostgresAccessTicketStore(connectionString, options);

        try
        {
            await store.EnsureTableExistsAsync(cancellationToken).ConfigureAwait(false);
            return store;
        }
        catch
        {
            await store.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    public static async Task<PostgresAccessTicketStore> CreateInitializedAsync(
        NpgsqlDataSource dataSource,
        PostgresAccessTicketStoreOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var store = new PostgresAccessTicketStore(dataSource, options);
        await store.EnsureTableExistsAsync(cancellationToken).ConfigureAwait(false);
        return store;
    }

    public static string BuildCreateTableSql(PostgresAccessTicketStoreOptions? options = null)
    {
        var resolvedOptions = options ?? new PostgresAccessTicketStoreOptions();
        var qualifiedTableName = resolvedOptions.GetQualifiedTableName();

        return
            $"""
            create table if not exists {qualifiedTableName} (
                environment text not null,
                service text not null,
                represented_cuit bigint not null,
                certificate_identifier text not null,
                token text not null,
                sign text not null,
                expires_at_utc timestamptz not null,
                stored_at_utc timestamptz not null,
                primary key (environment, service, represented_cuit, certificate_identifier)
            );
            """;
    }

    internal static long GetAdvisoryLockKey(AccessTicketStoreKey key)
    {
        ArgumentNullException.ThrowIfNull(key);

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(BuildKeyMaterial(key)));
        return BitConverter.ToInt64(hash, 0);
    }

    private static string BuildKeyMaterial(AccessTicketStoreKey key) =>
        $"{key.Environment}|{key.Service}|{key.RepresentedCuit}|{key.CertificateIdentifier}";

    private static NpgsqlDataSource CreateDataSource(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        return NpgsqlDataSource.Create(connectionString);
    }

    private static void AddKeyParameters(NpgsqlCommand command, AccessTicketStoreKey key)
    {
        command.Parameters.AddWithValue("environment", key.Environment.ToString());
        command.Parameters.AddWithValue("service", key.Service);
        command.Parameters.AddWithValue("represented_cuit", key.RepresentedCuit);
        command.Parameters.AddWithValue("certificate_identifier", key.CertificateIdentifier);
    }
}
