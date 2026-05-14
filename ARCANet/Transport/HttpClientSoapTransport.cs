using System.Net.Http.Headers;
using System.Text;
using ARCANet.Abstractions;

namespace ARCANet.Transport;

public sealed class HttpClientSoapTransport(HttpClient httpClient) : IArcaSoapTransport
{
    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

    public async Task<string> SendAsync(ArcaSoapRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var message = new HttpRequestMessage(HttpMethod.Post, request.Endpoint);
        message.Content = new StringContent(request.Body, Encoding.UTF8, "text/xml");

        if (!string.IsNullOrWhiteSpace(request.SoapAction))
        {
            message.Headers.TryAddWithoutValidation("SOAPAction", request.SoapAction);
        }

        using var response = await _httpClient.SendAsync(message, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return body;
    }
}
