using System.Xml.Linq;
using ARCANet.Authentication;

namespace ARCANet.Wsaa;

internal sealed class WsaaLoginResponseParser
{
    public AccessTicket Parse(string soapResponse)
    {
        ArgumentNullException.ThrowIfNull(soapResponse);

        var envelope = XDocument.Parse(soapResponse);
        var loginReturn = envelope
            .Descendants()
            .FirstOrDefault(x => x.Name.LocalName == "loginCmsReturn")
            ?.Value;

        if (string.IsNullOrWhiteSpace(loginReturn))
        {
            throw new InvalidOperationException("WSAA response does not contain loginCmsReturn.");
        }

        var ticketDocument = XDocument.Parse(loginReturn);
        var credentials = ticketDocument.Root?.Element("credentials");
        var header = ticketDocument.Root?.Element("header");

        var token = credentials?.Element("token")?.Value;
        var sign = credentials?.Element("sign")?.Value;
        var expirationTimeText = header?.Element("expirationTime")?.Value;

        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(sign) || string.IsNullOrWhiteSpace(expirationTimeText))
        {
            throw new InvalidOperationException("WSAA response is missing token, sign, or expiration time.");
        }

        return new AccessTicket(
            token,
            sign,
            DateTimeOffset.Parse(expirationTimeText, null, System.Globalization.DateTimeStyles.RoundtripKind));
    }
}
