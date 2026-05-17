using ARCANet.Authentication;
using ARCANet.Configuration;
using ARCANet.Persistence.Postgres;

namespace ARCANet.Tests.Authentication;

public sealed class PostgresAccessTicketStoreTests
{
    [Fact]
    public void BuildCreateTableSql_UsesExpectedPrimaryKey()
    {
        var sql = PostgresAccessTicketStore.BuildCreateTableSql();

        Assert.Contains("create table if not exists \"public\".\"arca_access_tickets\"", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("primary key (environment, service, represented_cuit, certificate_identifier)", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("expires_at_utc timestamptz not null", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CreateInitializedAsync_ConnectionStringOverload_IsAvailable()
    {
        var method = typeof(PostgresAccessTicketStore).GetMethod(
            nameof(PostgresAccessTicketStore.CreateInitializedAsync),
            [
                typeof(string),
                typeof(PostgresAccessTicketStoreOptions),
                typeof(CancellationToken)
            ]);

        Assert.NotNull(method);
    }

    [Fact]
    public void CreateInitializedAsync_DataSourceOverload_IsAvailable()
    {
        var method = typeof(PostgresAccessTicketStore).GetMethod(
            nameof(PostgresAccessTicketStore.CreateInitializedAsync),
            [
                typeof(Npgsql.NpgsqlDataSource),
                typeof(PostgresAccessTicketStoreOptions),
                typeof(CancellationToken)
            ]);

        Assert.NotNull(method);
    }

    [Fact]
    public void BuildCreateTableSql_UsesCustomSchemaAndTableNames()
    {
        var sql = PostgresAccessTicketStore.BuildCreateTableSql(new PostgresAccessTicketStoreOptions
        {
            SchemaName = "custom_schema",
            TableName = "custom_table"
        });

        Assert.Contains("create table if not exists \"custom_schema\".\"custom_table\"", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("bad-name")]
    [InlineData("bad.name")]
    [InlineData("123table")]
    [InlineData("table name")]
    public void BuildCreateTableSql_InvalidIdentifiers_Throw(string invalidIdentifier)
    {
        var options = new PostgresAccessTicketStoreOptions
        {
            SchemaName = invalidIdentifier,
            TableName = "arca_access_tickets"
        };

        Assert.Throws<ArgumentException>(() => PostgresAccessTicketStore.BuildCreateTableSql(options));
    }

    [Fact]
    public void GetAdvisoryLockKey_IsStableForSameKey()
    {
        var key = new AccessTicketStoreKey(
            ArcaEnvironment.Homologation,
            "wsfe",
            20123456789,
            "CERT-A");

        var first = PostgresAccessTicketStore.GetAdvisoryLockKey(key);
        var second = PostgresAccessTicketStore.GetAdvisoryLockKey(key);

        Assert.Equal(first, second);
    }

    [Fact]
    public void GetAdvisoryLockKey_DiffersByDiscriminatingFields()
    {
        var baseKey = new AccessTicketStoreKey(ArcaEnvironment.Homologation, "wsfe", 20123456789, "CERT-A");
        var environmentKey = new AccessTicketStoreKey(ArcaEnvironment.Production, "wsfe", 20123456789, "CERT-A");
        var serviceKey = new AccessTicketStoreKey(ArcaEnvironment.Homologation, "ws_sr_constancia_inscripcion", 20123456789, "CERT-A");
        var cuitKey = new AccessTicketStoreKey(ArcaEnvironment.Homologation, "wsfe", 20987654321, "CERT-A");
        var certificateKey = new AccessTicketStoreKey(ArcaEnvironment.Homologation, "wsfe", 20123456789, "CERT-B");

        Assert.NotEqual(PostgresAccessTicketStore.GetAdvisoryLockKey(baseKey), PostgresAccessTicketStore.GetAdvisoryLockKey(environmentKey));
        Assert.NotEqual(PostgresAccessTicketStore.GetAdvisoryLockKey(baseKey), PostgresAccessTicketStore.GetAdvisoryLockKey(serviceKey));
        Assert.NotEqual(PostgresAccessTicketStore.GetAdvisoryLockKey(baseKey), PostgresAccessTicketStore.GetAdvisoryLockKey(cuitKey));
        Assert.NotEqual(PostgresAccessTicketStore.GetAdvisoryLockKey(baseKey), PostgresAccessTicketStore.GetAdvisoryLockKey(certificateKey));
    }
}
