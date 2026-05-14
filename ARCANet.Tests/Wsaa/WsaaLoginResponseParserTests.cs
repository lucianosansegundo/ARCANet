using ARCANet.Wsaa;

namespace ARCANet.Tests.Wsaa;

public sealed class WsaaLoginResponseParserTests
{
    [Fact]
    public void Parse_ExtractsTokenSignAndExpiration()
    {
        const string soap = """
        <soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/">
          <soapenv:Body>
            <loginCmsResponse xmlns="http://wsaa.view.sua.dvadac.desein.afip.gov">
              <loginCmsReturn><![CDATA[<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <loginTicketResponse version="1.0">
          <header>
            <source>CN=wsaahomo</source>
            <destination>CN=test</destination>
            <uniqueId>1778760000</uniqueId>
            <generationTime>2026-05-14T11:50:00.000+00:00</generationTime>
            <expirationTime>2026-05-15T00:00:00.000+00:00</expirationTime>
          </header>
          <credentials>
            <token>TOKEN123</token>
            <sign>SIGN456</sign>
          </credentials>
        </loginTicketResponse>]]></loginCmsReturn>
            </loginCmsResponse>
          </soapenv:Body>
        </soapenv:Envelope>
        """;

        var parser = new WsaaLoginResponseParser();

        var ticket = parser.Parse(soap);

        Assert.Equal("TOKEN123", ticket.Token);
        Assert.Equal("SIGN456", ticket.Sign);
        Assert.Equal(new DateTimeOffset(2026, 5, 15, 0, 0, 0, TimeSpan.Zero), ticket.ExpiresAtUtc);
    }
}
