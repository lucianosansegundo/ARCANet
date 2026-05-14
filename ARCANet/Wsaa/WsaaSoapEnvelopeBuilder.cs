namespace ARCANet.Wsaa;

internal static class WsaaSoapEnvelopeBuilder
{
    public static string BuildLoginCmsEnvelope(string cmsBase64) =>
        $"""
        <soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/" xmlns:wsaa="http://wsaa.view.sua.dvadac.desein.afip.gov">
          <soapenv:Header/>
          <soapenv:Body>
            <wsaa:loginCms>
              <wsaa:in0>{System.Security.SecurityElement.Escape(cmsBase64)}</wsaa:in0>
            </wsaa:loginCms>
          </soapenv:Body>
        </soapenv:Envelope>
        """;
}
