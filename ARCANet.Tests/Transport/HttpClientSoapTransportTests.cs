using System.Net;
using System.Net.Http;
using ARCANet.Transport;

namespace ARCANet.Tests.Transport;

public sealed class HttpClientSoapTransportTests
{
    [Fact]
    public async Task SendAsync_IncludesSoapFaultBodyInException()
    {
        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("<soap:Fault><faultstring>cms.bad.base64</faultstring></soap:Fault>")
            });
        using var httpClient = new HttpClient(handler);
        var transport = new HttpClientSoapTransport(httpClient);

        var exception = await Assert.ThrowsAsync<ArcaSoapTransportException>(() =>
            transport.SendAsync(new ArcaSoapRequest(
                new Uri("https://example.test/ws"),
                "http://example.test/action",
                "<Envelope />")));

        Assert.Equal(HttpStatusCode.InternalServerError, exception.StatusCode);
        Assert.Contains("http://example.test/action", exception.Message, StringComparison.Ordinal);
        Assert.Contains("cms.bad.base64", exception.Message, StringComparison.Ordinal);
        Assert.Contains("<soap:Fault>", exception.ResponseBody, StringComparison.Ordinal);
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder = responder;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(_responder(request));
    }
}
