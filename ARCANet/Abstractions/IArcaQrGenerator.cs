using ARCANet.Qr;
using ARCANet.Invoices;

namespace ARCANet.Abstractions;

public interface IArcaQrGenerator
{
    ArcaQrPayload BuildPayload(AuthorizedInvoice invoice);
    string BuildJson(ArcaQrPayload payload);
    string BuildBase64(ArcaQrPayload payload);
    Uri BuildUrl(ArcaQrPayload payload);
    string BuildSvg(AuthorizedInvoice invoice, int pixelsPerModule = 20);
    string BuildSvg(ArcaQrPayload payload, int pixelsPerModule = 20);
    byte[] BuildPng(AuthorizedInvoice invoice, int pixelsPerModule = 20);
    byte[] BuildPng(ArcaQrPayload payload, int pixelsPerModule = 20);
}
