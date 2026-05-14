namespace ARCANet.Transport;

public sealed record ArcaSoapRequest(
    Uri Endpoint,
    string SoapAction,
    string Body);
