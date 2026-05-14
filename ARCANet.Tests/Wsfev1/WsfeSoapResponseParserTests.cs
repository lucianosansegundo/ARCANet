using ARCANet.Invoices;
using ARCANet.Wsfev1;

namespace ARCANet.Tests.Wsfev1;

public sealed class WsfeSoapResponseParserTests
{
    [Fact]
    public void ParseFeCaeSolicitar_ReadsApprovedResponse()
    {
        const string soap = """
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
                    <Concepto>1</Concepto>
                    <DocTipo>80</DocTipo>
                    <DocNro>30712345678</DocNro>
                    <CbteDesde>1234</CbteDesde>
                    <CbteHasta>1234</CbteHasta>
                    <Resultado>A</Resultado>
                    <CAE>70417054367476</CAE>
                    <CbteFch>20260514</CbteFch>
                    <CAEFchVto>20260524</CAEFchVto>
                  </FEDetResponse>
                </FeDetResp>
              </FECAESolicitarResult>
            </FECAESolicitarResponse>
          </soap:Body>
        </soap:Envelope>
        """;

        var parser = new WsfeSoapResponseParser();

        var response = parser.ParseFeCaeSolicitar(soap);

        Assert.Equal("A", response.HeaderResult);
        Assert.Equal("A", response.DetailResult);
        Assert.Equal("70417054367476", response.AuthorizationCode);
        Assert.Equal(new DateOnly(2026, 5, 24), response.AuthorizationDueDate);
    }

    [Fact]
    public void ParseLastAuthorizedNumber_ReadsCbteNro()
    {
        const string soap = """
        <soap:Envelope xmlns:soap="http://www.w3.org/2003/05/soap-envelope">
          <soap:Body>
            <FECompUltimoAutorizadoResponse>
              <FECompUltimoAutorizadoResult>
                <CbteNro>1234</CbteNro>
              </FECompUltimoAutorizadoResult>
            </FECompUltimoAutorizadoResponse>
          </soap:Body>
        </soap:Envelope>
        """;

        var parser = new WsfeSoapResponseParser();

        var result = parser.ParseLastAuthorizedNumber(soap);

        Assert.Equal(1234, result);
    }

    [Fact]
    public void ParseCompConsultar_ReadsFiscalFieldsFromOfficialShape()
    {
        const string soap = """
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
                  <Observaciones>
                    <Obs>
                      <Code>1000</Code>
                      <Msg>Observacion</Msg>
                    </Obs>
                  </Observaciones>
                </ResultGet>
              </FECompConsultarResult>
            </FECompConsultarResponse>
          </soap:Body>
        </soap:Envelope>
        """;

        var parser = new WsfeSoapResponseParser();

        var response = parser.ParseCompConsultar(soap);

        Assert.NotNull(response);
        Assert.Equal(20304050607, response!.IssuerCuit);
        Assert.Equal(5, response.PointOfSale);
        Assert.Equal(1, response.VoucherTypeCode);
        Assert.Equal(1234, response.VoucherNumber);
        Assert.Equal(InvoiceConcept.Services, response.Concept);
        Assert.Equal(80, response.CustomerDocumentTypeCode);
        Assert.Equal("30712345678", response.CustomerDocumentNumber);
        Assert.Equal(new DateOnly(2026, 5, 14), response.IssueDate);
        Assert.Equal(new DateOnly(2026, 5, 1), response.ServiceFrom);
        Assert.Equal(new DateOnly(2026, 5, 31), response.ServiceTo);
        Assert.Equal(new DateOnly(2026, 6, 14), response.PaymentDueDate);
        Assert.Equal(1210.00m, response.Totals.TotalAmount);
        Assert.Equal("PES", response.Currency.Code);
        Assert.Equal(1.00m, response.Currency.ExchangeRate);
        Assert.Single(response.VatItems);
        Assert.Single(response.Observations);
        Assert.Equal("1000", response.Observations[0].Code);
    }

    [Fact]
    public void ParseFeCaeSolicitar_ReadsNestedObservations()
    {
        const string soap = """
        <soap:Envelope xmlns:soap="http://www.w3.org/2003/05/soap-envelope">
          <soap:Body>
            <FECAESolicitarResponse>
              <FECAESolicitarResult>
                <FeCabResp>
                  <FchProceso>20260514120000</FchProceso>
                  <Resultado>R</Resultado>
                  <Reproceso>N</Reproceso>
                </FeCabResp>
                <FeDetResp>
                  <FEDetResponse>
                    <Resultado>R</Resultado>
                    <Observaciones>
                      <Obs>
                        <Code>10197</Code>
                        <Msg>Condicion IVA invalida</Msg>
                      </Obs>
                    </Observaciones>
                  </FEDetResponse>
                </FeDetResp>
              </FECAESolicitarResult>
            </FECAESolicitarResponse>
          </soap:Body>
        </soap:Envelope>
        """;

        var parser = new WsfeSoapResponseParser();

        var response = parser.ParseFeCaeSolicitar(soap);

        Assert.Single(response.Observations);
        Assert.Equal("10197", response.Observations[0].Code);
    }
}
