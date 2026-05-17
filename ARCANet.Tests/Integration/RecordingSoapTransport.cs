using ARCANet.Abstractions;
using ARCANet.Transport;

namespace ARCANet.Tests.Integration;

public sealed class RecordingSoapTransport(IArcaSoapTransport innerTransport) : IArcaSoapTransport
{
    private readonly IArcaSoapTransport _innerTransport = innerTransport ?? throw new ArgumentNullException(nameof(innerTransport));

    public ArcaSoapRequest? LastRequest { get; private set; }

    public string? LastResponseBody { get; private set; }

    public Exception? LastException { get; private set; }

    public async Task<string> SendAsync(ArcaSoapRequest request, CancellationToken cancellationToken = default)
    {
        LastRequest = request;
        LastResponseBody = null;
        LastException = null;

        try
        {
            var response = await _innerTransport.SendAsync(request, cancellationToken).ConfigureAwait(false);
            LastResponseBody = response;
            return response;
        }
        catch (Exception exception)
        {
            LastException = exception;

            if (exception is ArcaSoapTransportException soapException)
            {
                LastResponseBody = soapException.ResponseBody;
            }

            throw;
        }
    }
}
