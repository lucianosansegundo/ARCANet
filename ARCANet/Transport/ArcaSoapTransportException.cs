using System.Net;

namespace ARCANet.Transport;

public sealed class ArcaSoapTransportException : HttpRequestException
{
    public ArcaSoapTransportException(
        Uri endpoint,
        string soapAction,
        HttpStatusCode statusCode,
        string? responseBody,
        Exception? innerException = null)
        : base(BuildMessage(endpoint, soapAction, statusCode, responseBody), innerException, statusCode)
    {
        Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        SoapAction = soapAction ?? string.Empty;
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }

    public Uri Endpoint { get; }

    public string SoapAction { get; }

    public new HttpStatusCode StatusCode { get; }

    public string? ResponseBody { get; }

    private static string BuildMessage(
        Uri endpoint,
        string soapAction,
        HttpStatusCode statusCode,
        string? responseBody)
    {
        var summary = $"SOAP request failed with HTTP {(int)statusCode} ({statusCode}) for '{soapAction}' at '{endpoint}'.";
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return summary;
        }

        var normalizedBody = responseBody
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Trim();

        if (normalizedBody.Length > 1000)
        {
            normalizedBody = $"{normalizedBody[..1000]}...";
        }

        return $"{summary} Response body: {normalizedBody}";
    }
}
