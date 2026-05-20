using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using ARCANet.Abstractions;
using ARCANet.Authentication;
using ARCANet.Taxpayers;
using ARCANet.Transport;

namespace ARCANet.Tests.Taxpayers;

public sealed class TaxpayerRegistryClientTests
{
    [Fact]
    public async Task GetTaxpayerAsync_SendsRepresentedAndTargetCuit()
    {
        var certificateProvider = new FakeCertificateProvider("SERIALNUMBER=CUIT 20123456789, CN=ARCANet Test");
        var accessTicketProvider = new FakeAccessTicketProvider();
        var transport = new FakeSoapTransport(
            """
            <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
              <soap:Body>
                <ns2:getPersona_v2Response xmlns:ns2="http://a5.soap.ws.server.puc.sr/">
                  <personaReturn>
                    <datosGenerales>
                      <idPersona>30712345678</idPersona>
                      <tipoPersona>JURIDICA</tipoPersona>
                      <estadoClave>ACTIVO</estadoClave>
                      <razonSocial>CLIENTE SA</razonSocial>
                    </datosGenerales>
                    <datosRegimenGeneral>
                      <impuesto>
                        <descripcionImpuesto>IVA</descripcionImpuesto>
                        <idImpuesto>30</idImpuesto>
                        <estadoImpuesto>AC</estadoImpuesto>
                        <periodo>201901</periodo>
                      </impuesto>
                    </datosRegimenGeneral>
                  </personaReturn>
                </ns2:getPersona_v2Response>
              </soap:Body>
            </soap:Envelope>
            """);
        var client = new TaxpayerRegistryClient(
            certificateProvider,
            accessTicketProvider,
            transport);

        var profile = await client.GetTaxpayerAsync(30712345678);

        Assert.NotNull(profile);
        Assert.Equal("ws_sr_constancia_inscripcion", accessTicketProvider.LastService);
        var request = Assert.Single(transport.Requests);
        Assert.Equal("https://awshomo.afip.gov.ar/sr-padron/webservices/personaServiceA5", request.Endpoint.ToString());
        Assert.Contains("<cuitRepresentada>20123456789</cuitRepresentada>", request.Body, StringComparison.Ordinal);
        Assert.Contains("<idPersona>30712345678</idPersona>", request.Body, StringComparison.Ordinal);
        Assert.Contains("<token>TOKEN123</token>", request.Body, StringComparison.Ordinal);
        Assert.Contains("<sign>SIGN456</sign>", request.Body, StringComparison.Ordinal);
    }

    private sealed class FakeAccessTicketProvider : IAccessTicketProvider
    {
        public string? LastService { get; private set; }

        public Task<AccessTicket> GetAccessTicketAsync(string service, CancellationToken cancellationToken = default)
        {
            LastService = service;
            return Task.FromResult(new AccessTicket("TOKEN123", "SIGN456", DateTimeOffset.UtcNow.AddHours(1)));
        }
    }

    private sealed class FakeCertificateProvider(string subjectName) : ICertificateProvider
    {
        private readonly string _subjectName = subjectName;

        public Task<X509Certificate2> GetCertificateAsync(CancellationToken cancellationToken = default)
        {
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest(
                _subjectName,
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            return Task.FromResult(request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1)));
        }
    }

    private sealed class FakeSoapTransport(params string[] responses) : IArcaSoapTransport
    {
        private readonly Queue<string> _responses = new(responses);

        public List<ArcaSoapRequest> Requests { get; } = [];

        public Task<string> SendAsync(ArcaSoapRequest request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return Task.FromResult(_responses.Dequeue());
        }
    }
}
