using ARCANet.Abstractions;

namespace ARCANet.Authentication;

public sealed class NullAccessTicketStore : IAccessTicketStore
{
    public Task<StoredAccessTicket?> GetAsync(
        AccessTicketStoreKey key,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        return Task.FromResult<StoredAccessTicket?>(null);
    }

    public Task SaveAsync(
        AccessTicketStoreKey key,
        StoredAccessTicket ticket,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(ticket);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(
        AccessTicketStoreKey key,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        return Task.CompletedTask;
    }
}
