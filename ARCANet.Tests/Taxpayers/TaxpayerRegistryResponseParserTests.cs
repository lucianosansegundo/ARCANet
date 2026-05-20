using ARCANet.Invoices;
using ARCANet.Taxpayers;

namespace ARCANet.Tests.Taxpayers;

public sealed class TaxpayerRegistryResponseParserTests
{
    [Fact]
    public void ParseGetPersonaResponse_MapsResponsibleInscribedProfile()
    {
        const string soap = """
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
                  <impuesto>
                    <descripcionImpuesto>GANANCIAS SOCIEDADES</descripcionImpuesto>
                    <idImpuesto>10</idImpuesto>
                    <estadoImpuesto>AC</estadoImpuesto>
                    <periodo>201901</periodo>
                  </impuesto>
                </datosRegimenGeneral>
              </personaReturn>
            </ns2:getPersona_v2Response>
          </soap:Body>
        </soap:Envelope>
        """;

        var parser = new TaxpayerRegistryResponseParser();

        var profile = parser.ParseGetPersonaResponse(soap);

        Assert.NotNull(profile);
        Assert.Equal(30712345678, profile!.Cuit);
        Assert.Equal("CLIENTE SA", profile.DisplayName);
        Assert.Equal(TaxpayerVatStatus.ResponsibleInscribed, profile.VatStatus);
        Assert.Equal(new ReceiverVatCondition(1, "IVA Responsable Inscripto"), profile.SuggestedReceiverVatCondition);
        Assert.Equal(2, profile.GeneralTaxes.Count);
        Assert.Null(profile.Monotributo);
    }

    [Fact]
    public void ParseGetPersonaResponse_MapsMonotributoProfile()
    {
        const string soap = """
        <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
          <soap:Body>
            <ns2:getPersona_v2Response xmlns:ns2="http://a5.soap.ws.server.puc.sr/">
              <personaReturn>
                <datosGenerales>
                  <idPersona>20333444556</idPersona>
                  <tipoPersona>FISICA</tipoPersona>
                  <estadoClave>ACTIVO</estadoClave>
                  <nombre>ANA</nombre>
                  <apellido>PEREZ</apellido>
                </datosGenerales>
                <datosMonotributo>
                  <impuesto>
                    <descripcionImpuesto>MONOTRIBUTO</descripcionImpuesto>
                    <idImpuesto>20</idImpuesto>
                    <estadoImpuesto>AC</estadoImpuesto>
                    <periodo>202401</periodo>
                  </impuesto>
                  <categoriaMonotributo>
                    <descripcionCategoria>CATEGORIA B</descripcionCategoria>
                    <idCategoria>62</idCategoria>
                    <idImpuesto>20</idImpuesto>
                    <periodo>202401</periodo>
                  </categoriaMonotributo>
                </datosMonotributo>
              </personaReturn>
            </ns2:getPersona_v2Response>
          </soap:Body>
        </soap:Envelope>
        """;

        var parser = new TaxpayerRegistryResponseParser();

        var profile = parser.ParseGetPersonaResponse(soap);

        Assert.NotNull(profile);
        Assert.Equal(20333444556, profile!.Cuit);
        Assert.Equal("ANA PEREZ", profile.DisplayName);
        Assert.Equal(TaxpayerVatStatus.Monotributista, profile.VatStatus);
        Assert.Equal(new ReceiverVatCondition(6, "Responsable Monotributo"), profile.SuggestedReceiverVatCondition);
        Assert.NotNull(profile.Monotributo);
        Assert.Equal("CATEGORIA B", profile.Monotributo!.CategoryName);
    }
}
