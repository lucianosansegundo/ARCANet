using System.Collections.Concurrent;
using ARCANet.Abstractions;

namespace ARCANet.Authentication;

public sealed class InMemoryAccessTicketStore : IAccessTicketStore
{
    private readonly ConcurrentDictionary<AccessTicketStoreKey, StoredAccessTicket> _tickets = new();

    public Task<StoredAccessTicket?> GetAsync(
        AccessTicketStoreKey key,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        _tickets.TryGetValue(key, out var ticket);
        return Task.FromResult(ticket);
    }

    public Task SaveAsync(
        AccessTicketStoreKey key,
        StoredAccessTicket ticket,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(ticket);

        _tickets[key] = ticket;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(
        AccessTicketStoreKey key,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        _tickets.TryRemove(key, out _);
        return Task.CompletedTask;
    }
}
