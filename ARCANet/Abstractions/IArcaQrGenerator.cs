using ARCANet.Qr;
using ARCANet.Invoices;

namespace ARCANet.Abstractions;

public interface IArcaQrGenerator
{
    ArcaQrPayload BuildPayload(AuthorizedInvoice invoice);
    string BuildJson(ArcaQrPayload payload);
    string BuildBase64(ArcaQrPayload payload);
    Uri BuildUrl(ArcaQrPayload payload);
}
