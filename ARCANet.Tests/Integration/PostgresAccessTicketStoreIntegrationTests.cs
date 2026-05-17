using ARCANet.Authentication;
using ARCANet.Configuration;
using ARCANet.Persistence.Postgres;
using Testcontainers.PostgreSql;

namespace ARCANet.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class PostgresAccessTicketStoreIntegrationTests
{
    [PostgresIntegrationFact]
    public async Task EnsureTableExistsAsync_SaveGetAndDelete_WorkEndToEnd()
    {
        await using var container = await StartContainerAsync();
        var options = CreateUniqueOptions();
        await using var store = new PostgresAccessTicketStore(container.GetConnectionString(), options);
        await store.EnsureTableExistsAsync();

        var key = new AccessTicketStoreKey(
            ArcaEnvironment.Homologation,
            "wsfe",
            20123456789,
            "CERT-A");
        var ticket = new StoredAccessTicket(
            "TOKEN123",
            "SIGN456",
            new DateTimeOffset(2026, 5, 20, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 5, 16, 0, 0, 0, TimeSpan.Zero));

        await store.SaveAsync(key, ticket);
        var loaded = await store.GetAsync(key);
        await store.DeleteAsync(key);
        var deleted = await store.GetAsync(key);

        Assert.Equal(ticket, loaded);
        Assert.Null(deleted);
    }

    [PostgresIntegrationFact]
    public async Task SaveAsync_UpsertsTicket()
    {
        await using var container = await StartContainerAsync();
        var options = CreateUniqueOptions();
        await using var store = await PostgresAccessTicketStore.CreateInitializedAsync(container.GetConnectionString(), options);

        var key = new AccessTicketStoreKey(
            ArcaEnvironment.Homologation,
            "wsfe",
            20123456789,
            "CERT-A");

        await store.SaveAsync(
            key,
            new StoredAccessTicket(
                "TOKEN-A",
                "SIGN-A",
                new DateTimeOffset(2026, 5, 20, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 5, 16, 0, 0, 0, TimeSpan.Zero)));

        await store.SaveAsync(
            key,
            new StoredAccessTicket(
                "TOKEN-B",
                "SIGN-B",
                new DateTimeOffset(2026, 5, 21, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 5, 16, 0, 0, 1, TimeSpan.Zero)));

        var loaded = await store.GetAsync(key);

        Assert.NotNull(loaded);
        Assert.Equal("TOKEN-B", loaded!.Token);
        Assert.Equal("SIGN-B", loaded.Sign);
    }

    [PostgresIntegrationFact]
    public async Task ExecuteSerializedAsync_TwoStores_SerializeByKey()
    {
        await using var container = await StartContainerAsync();
        var options = CreateUniqueOptions();
        await using var firstStore = await PostgresAccessTicketStore.CreateInitializedAsync(container.GetConnectionString(), options);
        await using var secondStore = new PostgresAccessTicketStore(container.GetConnectionString(), options);

        var key = new AccessTicketStoreKey(
            ArcaEnvironment.Homologation,
            "wsfe",
            20123456789,
            "CERT-A");

        var concurrentExecutions = 0;
        var maxConcurrentExecutions = 0;

        var firstTask = firstStore.ExecuteSerializedAsync(
            key,
            async _ =>
            {
                var current = Interlocked.Increment(ref concurrentExecutions);
                maxConcurrentExecutions = Math.Max(maxConcurrentExecutions, current);
                await Task.Delay(75).ConfigureAwait(false);
                Interlocked.Decrement(ref concurrentExecutions);
                return 1;
            });

        var secondTask = secondStore.ExecuteSerializedAsync(
            key,
            async _ =>
            {
                var current = Interlocked.Increment(ref concurrentExecutions);
                maxConcurrentExecutions = Math.Max(maxConcurrentExecutions, current);
                await Task.Delay(75).ConfigureAwait(false);
                Interlocked.Decrement(ref concurrentExecutions);
                return 2;
            });

        var results = await Task.WhenAll(firstTask, secondTask);

        Assert.Equal([1, 2], results.Order().ToArray());
        Assert.Equal(1, maxConcurrentExecutions);
    }

    private static PostgresAccessTicketStoreOptions CreateUniqueOptions() =>
        new()
        {
            SchemaName = "public",
            TableName = $"arca_access_tickets_{Guid.NewGuid():N}"
        };

    private static async Task<PostgreSqlContainer> StartContainerAsync()
    {
        var container = new PostgreSqlBuilder(PostgresIntegrationTestSettings.GetImage())
            .WithDatabase("arcanet_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        await container.StartAsync().ConfigureAwait(false);
        return container;
    }
}
