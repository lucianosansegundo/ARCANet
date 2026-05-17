using ARCANet.Authentication;
using ARCANet.Configuration;

namespace ARCANet.Tests.Authentication;

public sealed class FileAccessTicketStoreTests
{
    [Fact]
    public async Task SaveAsync_ThenGetAsync_RoundTripsTicket()
    {
        using var fixture = new TemporaryDirectoryFixture();
        var store = new FileAccessTicketStore(fixture.DirectoryPath);
        var key = new AccessTicketStoreKey(
            ArcaEnvironment.Homologation,
            "wsfe",
            20123456789,
            "THUMBPRINT123");
        var ticket = new StoredAccessTicket(
            "TOKEN123",
            "SIGN456",
            new DateTimeOffset(2026, 5, 17, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 5, 16, 0, 0, 0, TimeSpan.Zero));

        await store.SaveAsync(key, ticket);
        var loaded = await store.GetAsync(key);

        Assert.Equal(ticket, loaded);
    }

    [Fact]
    public async Task DeleteAsync_RemovesPersistedTicket()
    {
        using var fixture = new TemporaryDirectoryFixture();
        var store = new FileAccessTicketStore(fixture.DirectoryPath);
        var key = new AccessTicketStoreKey(
            ArcaEnvironment.Homologation,
            "wsfe",
            20123456789,
            "THUMBPRINT123");

        await store.SaveAsync(
            key,
            new StoredAccessTicket(
                "TOKEN123",
                "SIGN456",
                new DateTimeOffset(2026, 5, 17, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 5, 16, 0, 0, 0, TimeSpan.Zero)));

        await store.DeleteAsync(key);
        var loaded = await store.GetAsync(key);

        Assert.Null(loaded);
    }

    [Fact]
    public async Task SaveAsync_UsesDifferentFilesForDifferentKeys()
    {
        using var fixture = new TemporaryDirectoryFixture();
        var store = new FileAccessTicketStore(fixture.DirectoryPath);

        await store.SaveAsync(
            new AccessTicketStoreKey(ArcaEnvironment.Homologation, "wsfe", 20123456789, "CERT-A"),
            new StoredAccessTicket("TOKEN-A", "SIGN-A", new DateTimeOffset(2026, 5, 17, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 5, 16, 0, 0, 0, TimeSpan.Zero)));
        await store.SaveAsync(
            new AccessTicketStoreKey(ArcaEnvironment.Production, "wsfe", 20123456789, "CERT-A"),
            new StoredAccessTicket("TOKEN-B", "SIGN-B", new DateTimeOffset(2026, 5, 18, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 5, 16, 0, 0, 0, TimeSpan.Zero)));

        var files = Directory.GetFiles(fixture.DirectoryPath, "*.json");

        Assert.Equal(2, files.Length);
    }

    [Fact]
    public async Task SaveAsync_ConcurrentWritesForSameKey_LeavesReadableTicket()
    {
        using var fixture = new TemporaryDirectoryFixture();
        var firstStore = new FileAccessTicketStore(fixture.DirectoryPath);
        var secondStore = new FileAccessTicketStore(fixture.DirectoryPath);
        var key = new AccessTicketStoreKey(
            ArcaEnvironment.Homologation,
            "wsfe",
            20123456789,
            "THUMBPRINT123");

        var firstSave = firstStore.SaveAsync(
            key,
            new StoredAccessTicket(
                "TOKEN-A",
                "SIGN-A",
                new DateTimeOffset(2026, 5, 17, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 5, 16, 0, 0, 0, TimeSpan.Zero)));
        var secondSave = secondStore.SaveAsync(
            key,
            new StoredAccessTicket(
                "TOKEN-B",
                "SIGN-B",
                new DateTimeOffset(2026, 5, 18, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 5, 16, 0, 0, 1, TimeSpan.Zero)));

        await Task.WhenAll(firstSave, secondSave);
        var loaded = await firstStore.GetAsync(key);

        Assert.NotNull(loaded);
        Assert.True(loaded!.Token is "TOKEN-A" or "TOKEN-B");
        Assert.True(loaded.Sign is "SIGN-A" or "SIGN-B");
    }

    [Fact]
    public async Task ExecuteSerializedAsync_TwoStoreInstances_SerializeByKey()
    {
        using var fixture = new TemporaryDirectoryFixture();
        var firstStore = new FileAccessTicketStore(fixture.DirectoryPath);
        var secondStore = new FileAccessTicketStore(fixture.DirectoryPath);
        var key = new AccessTicketStoreKey(
            ArcaEnvironment.Homologation,
            "wsfe",
            20123456789,
            "THUMBPRINT123");

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

    private sealed class TemporaryDirectoryFixture : IDisposable
    {
        public TemporaryDirectoryFixture()
        {
            DirectoryPath = Path.Combine(Path.GetTempPath(), "ARCANet.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(DirectoryPath);
        }

        public string DirectoryPath { get; }

        public void Dispose()
        {
            if (Directory.Exists(DirectoryPath))
            {
                Directory.Delete(DirectoryPath, recursive: true);
            }
        }
    }
}
