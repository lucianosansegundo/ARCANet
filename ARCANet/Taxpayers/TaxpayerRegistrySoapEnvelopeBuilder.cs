using System.Security;
using ARCANet.Authentication;

namespace ARCANet.Taxpayers;

internal static class TaxpayerRegistrySoapEnvelopeBuilder
{
    public static string BuildGetPersonaEnvelope(AccessTicket ticket, long representedCuit, long taxpayerCuit)
    {
        ArgumentNullException.ThrowIfNull(ticket);

        return
            $"""
            <soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/" xmlns:a5="http://a5.soap.ws.server.puc.sr/">
              <soapenv:Header/>
              <soapenv:Body>
                <a5:getPersona_v2>
                  <token>{SecurityElement.Escape(ticket.Token)}</token>
                  <sign>{SecurityElement.Escape(ticket.Sign)}</sign>
                  <cuitRepresentada>{representedCuit}</cuitRepresentada>
                  <idPersona>{taxpayerCuit}</idPersona>
                </a5:getPersona_v2>
              </soapenv:Body>
            </soapenv:Envelope>
            """;
    }
}
