using ARCANet.Authentication;

namespace ARCANet.Abstractions;

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
