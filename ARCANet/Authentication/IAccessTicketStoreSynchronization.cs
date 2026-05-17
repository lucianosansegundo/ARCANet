namespace ARCANet.Authentication;

public interface IAccessTicketStoreSynchronization
{
    Task<T> ExecuteSerializedAsync<T>(
        AccessTicketStoreKey key,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default);
}
