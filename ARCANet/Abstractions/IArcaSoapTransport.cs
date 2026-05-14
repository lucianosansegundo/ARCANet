using ARCANet.Transport;

namespace ARCANet.Abstractions;

public interface IArcaSoapTransport
{
    Task<string> SendAsync(
        ArcaSoapRequest request,
        CancellationToken cancellationToken = default);
}
