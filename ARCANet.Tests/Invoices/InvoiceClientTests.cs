using ARCANet.Abstractions;
using ARCANet.Authentication;
using ARCANet.Invoices;
using ARCANet.Qr;
using ARCANet.Transport;
using ARCANet.Wsaa;
using ARCANet.Wsfev1;

namespace ARCANet.Tests.Invoices;

public sealed class InvoiceClientTests
{
    [Fact]
    public async Task CreateInvoiceAsync_ReturnsAuthorizedResultAndGeneratesQr()
    {
        var request = TestInvoiceFactory.CreateValidFacturaARequest();
        var validator = new InvoiceRequestValidator(new FakeClock());
        var transport = new FakeSoapTransport(
            BuildWsaaResponse(),
            BuildWsfeAuthorizationResponse());
        var accessTickets = new WsaaAccessTicketProvider(
            new FakeCertificateProvider(),
            transport,
            new FakeClock());
        var client = new InvoiceClient(
            validator,
            new ArcaQrGenerator(),
            accessTickets,
            transport);

        var result = await client.CreateInvoiceAsync(request);

        var authorized = Assert.IsType<AuthorizedInvoiceResult>(result);
        Assert.Equal("70417054367476", authorized.Invoice.AuthorizationCode);
        Assert.NotNull(authorized.Invoice.QrPayload);
        Assert.NotNull(authorized.Invoice.QrUrl);
        Assert.Collection(
            transport.Requests,
            request => Assert.Equal("http://wsaa.view.sua.dvadac.desein.afip.gov/loginCms", request.SoapAction),
            request => Assert.Equal("http://ar.gov.afip.dif.FEV1/FECAESolicitar", request.SoapAction));
    }

    [Fact]
    public async Task CreateInvoiceAsync_ReturnsStableUnknownResultForTechnicalFailure()
    {
        var request = TestInvoiceFactory.CreateValidFacturaARequest();
        var validator = new InvoiceRequestValidator(new FakeClock());
        var transport = new FakeSoapTransport(
            BuildWsaaResponse(),
            new InvalidOperationException("transport failure"));
        var accessTickets = new WsaaAccessTicketProvider(
            new FakeCertificateProvider(),
            transport,
            new FakeClock());
        var client = new InvoiceClient(
            validator,
            new ArcaQrGenerator(),
            accessTickets,
            transport);

        var result = await client.CreateInvoiceAsync(request);

        var unknown = Assert.IsType<UnknownInvoiceResult>(result);
        Assert.Equal("Invoice submission could not be confirmed. Query before retrying.", unknown.Reason);
        Assert.True(unknown.ShouldQueryBeforeRetry);
    }

    [Fact]
    public async Task CreateInvoiceAsync_PropagatesCancellation()
    {
        var request = TestInvoiceFactory.CreateValidFacturaARequest();
        var validator = new InvoiceRequestValidator(new FakeClock());
        var transport = new FakeSoapTransport(
            BuildWsaaResponse(),
            new OperationCanceledException("cancelled"));
        var accessTickets = new WsaaAccessTicketProvider(
            new FakeCertificateProvider(),
            transport,
            new FakeClock());
        var client = new InvoiceClient(
            validator,
            new ArcaQrGenerator(),
            accessTickets,
            transport);

        await Assert.ThrowsAsync<OperationCanceledException>(() => client.CreateInvoiceAsync(request));
    }

    [Fact]
    public async Task GetInvoiceAsync_MapsConsultarResponseWithoutInventingTotals()
    {
        var validator = new InvoiceRequestValidator(new FakeClock());
        var transport = new FakeSoapTransport(
            BuildWsaaResponse(),
            BuildWsfeConsultarResponse());
        var accessTickets = new WsaaAccessTicketProvider(
            new FakeCertificateProvider(),
            transport,
            new FakeClock());
        var client = new InvoiceClient(
            validator,
            new ArcaQrGenerator(),
            accessTickets,
            transport);
        var locator = new InvoiceLocator(
            new VoucherSeries(20304050607, 5, new VoucherType(1, "Factura A")),
            1234);

        var invoice = await client.GetInvoiceAsync(locator);

        Assert.NotNull(invoice);
        Assert.Equal(InvoiceConcept.Services, invoice!.Concept);
        Assert.Equal("30712345678", invoice.Customer.DocumentNumber);
        Assert.Equal(1210.00m, invoice.Totals.TotalAmount);
        Assert.Equal(new DateOnly(2026, 6, 14), invoice.PaymentDueDate);
        Assert.Equal(AuthorizationCodeType.Cae, invoice.AuthorizationCodeType);
        Assert.Single(invoice.VatItems);
        Assert.NotNull(invoice.QrUrl);
    }

    private static string BuildWsaaResponse() =>
        """
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

    private static string BuildWsfeAuthorizationResponse() =>
        """
        <soap:Envelope xmlns:soap="http://www.w3.org/2003/05/soap-envelope">
          <soap:Body>
            <FECAESolicitarResponse>
              <FECAESolicitarResult>
                <FeCabResp>
                  <Cuit>20304050607</Cuit>
                  <PtoVta>5</PtoVta>
                  <CbteTipo>1</CbteTipo>
                  <FchProceso>20260514120000</FchProceso>
                  <CantReg>1</CantReg>
                  <Resultado>A</Resultado>
                  <Reproceso>N</Reproceso>
                </FeCabResp>
                <FeDetResp>
                  <FEDetResponse>
                    <Resultado>A</Resultado>
                    <CAE>70417054367476</CAE>
                    <CAEFchVto>20260524</CAEFchVto>
                  </FEDetResponse>
                </FeDetResp>
              </FECAESolicitarResult>
            </FECAESolicitarResponse>
          </soap:Body>
        </soap:Envelope>
        """;

    private static string BuildWsfeConsultarResponse() =>
        """
        <soap:Envelope xmlns:soap="http://www.w3.org/2003/05/soap-envelope">
          <soap:Body>
            <FECompConsultarResponse>
              <FECompConsultarResult>
                <ResultGet>
                  <Concepto>2</Concepto>
                  <DocTipo>80</DocTipo>
                  <DocNro>30712345678</DocNro>
                  <CbteDesde>1234</CbteDesde>
                  <CbteHasta>1234</CbteHasta>
                  <CbteFch>20260514</CbteFch>
                  <ImpTotal>1210.00</ImpTotal>
                  <ImpTotConc>0.00</ImpTotConc>
                  <ImpNeto>1000.00</ImpNeto>
                  <ImpOpEx>0.00</ImpOpEx>
                  <ImpTrib>0.00</ImpTrib>
                  <ImpIVA>210.00</ImpIVA>
                  <FchServDesde>20260501</FchServDesde>
                  <FchServHasta>20260531</FchServHasta>
                  <FchVtoPago>20260614</FchVtoPago>
                  <MonId>PES</MonId>
                  <MonCotiz>1.00</MonCotiz>
                  <Resultado>A</Resultado>
                  <CodAutorizacion>70417054367476</CodAutorizacion>
                  <EmisionTipo>CAE</EmisionTipo>
                  <FchVto>20260524</FchVto>
                  <FchProceso>20260514120000</FchProceso>
                  <PtoVta>5</PtoVta>
                  <CbteTipo>1</CbteTipo>
                  <Cuit>20304050607</Cuit>
                  <Iva>
                    <AlicIva>
                      <Id>5</Id>
                      <BaseImp>1000.00</BaseImp>
                      <Importe>210.00</Importe>
                    </AlicIva>
                  </Iva>
                </ResultGet>
              </FECompConsultarResult>
            </FECompConsultarResponse>
          </soap:Body>
        </soap:Envelope>
        """;

    private sealed class FakeClock : IClock
    {
        public DateTimeOffset UtcNow => new(2026, 5, 14, 12, 0, 0, TimeSpan.Zero);
    }

    private sealed class FakeSoapTransport(params object[] responses) : IArcaSoapTransport
    {
        private readonly Queue<object> _responses = new(responses);
        public List<ArcaSoapRequest> Requests { get; } = [];

        public Task<string> SendAsync(ArcaSoapRequest request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request);

            if (_responses.Count == 0)
            {
                throw new InvalidOperationException("No more fake SOAP responses configured.");
            }

            var next = _responses.Dequeue();
            return next switch
            {
                string response => Task.FromResult(response),
                Exception exception => Task.FromException<string>(exception),
                _ => throw new InvalidOperationException("Unsupported fake SOAP response type.")
            };
        }
    }

    private sealed class FakeCertificateProvider : ICertificateProvider
    {
        public Task<System.Security.Cryptography.X509Certificates.X509Certificate2> GetCertificateAsync(CancellationToken cancellationToken = default)
        {
            using var rsa = System.Security.Cryptography.RSA.Create(2048);
            var request = new System.Security.Cryptography.X509Certificates.CertificateRequest(
                "CN=ARCANet Test",
                rsa,
                System.Security.Cryptography.HashAlgorithmName.SHA256,
                System.Security.Cryptography.RSASignaturePadding.Pkcs1);

            var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
            return Task.FromResult(certificate);
        }
    }
}
