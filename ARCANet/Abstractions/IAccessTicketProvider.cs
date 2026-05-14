using ARCANet.Authentication;

namespace ARCANet.Abstractions;

public interface IAccessTicketProvider
{
    Task<AccessTicket> GetAccessTicketAsync(
        string service,
        CancellationToken cancellationToken = default);
}
