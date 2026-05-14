using System.Globalization;
using System.Text;
using ARCANet.Authentication;
using ARCANet.InternalInvoices;

namespace ARCANet.Wsfev1;

internal sealed class WsfeSoapEnvelopeBuilder(Wsfev1Options options)
{
    private readonly Wsfev1Options _options = options ?? throw new ArgumentNullException(nameof(options));

    public string BuildFeCompUltimoAutorizado(AccessTicket ticket, long issuerCuit, int pointOfSale, int voucherTypeCode) =>
        $"""
        <soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/" xmlns:ar="http://ar.gov.afip.dif.FEV1/">
          <soapenv:Header/>
          <soapenv:Body>
            <ar:FECompUltimoAutorizado>
              {BuildAuth(ticket, issuerCuit)}
              <ar:PtoVta>{pointOfSale}</ar:PtoVta>
              <ar:CbteTipo>{voucherTypeCode}</ar:CbteTipo>
            </ar:FECompUltimoAutorizado>
          </soapenv:Body>
        </soapenv:Envelope>
        """;

    public string BuildFeCompConsultar(AccessTicket ticket, long issuerCuit, int pointOfSale, int voucherTypeCode, long voucherNumber) =>
        $"""
        <soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/" xmlns:ar="http://ar.gov.afip.dif.FEV1/">
          <soapenv:Header/>
          <soapenv:Body>
            <ar:FECompConsultar>
              {BuildAuth(ticket, issuerCuit)}
              <ar:FeCompConsReq>
                <ar:CbteTipo>{voucherTypeCode}</ar:CbteTipo>
                <ar:CbteNro>{voucherNumber}</ar:CbteNro>
                <ar:PtoVta>{pointOfSale}</ar:PtoVta>
              </ar:FeCompConsReq>
            </ar:FECompConsultar>
          </soapenv:Body>
        </soapenv:Envelope>
        """;

    public string BuildFeCaeSolicitar(AccessTicket ticket, InternalInvoiceSubmission submission)
    {
        ArgumentNullException.ThrowIfNull(submission);

        var (docType, docNumber) = ResolveCustomerDocument(submission);
        var sb = new StringBuilder();

        sb.Append("""
        <soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/" xmlns:ar="http://ar.gov.afip.dif.FEV1/">
          <soapenv:Header/>
          <soapenv:Body>
            <ar:FECAESolicitar>
        """);
        sb.Append(BuildAuth(ticket, submission.IssuerCuit));
        sb.Append($"""
              <ar:FeCAEReq>
                <ar:FeCabReq>
                  <ar:CantReg>1</ar:CantReg>
                  <ar:PtoVta>{submission.Series.PointOfSale}</ar:PtoVta>
                  <ar:CbteTipo>{submission.Series.VoucherType.Code}</ar:CbteTipo>
                </ar:FeCabReq>
                <ar:FeDetReq>
                  <ar:FECAEDetRequest>
                    <ar:Concepto>{(int)submission.Concept}</ar:Concepto>
                    <ar:DocTipo>{docType}</ar:DocTipo>
                    <ar:DocNro>{docNumber}</ar:DocNro>
                    <ar:CbteDesde>{submission.VoucherNumber}</ar:CbteDesde>
                    <ar:CbteHasta>{submission.VoucherNumber}</ar:CbteHasta>
                    <ar:CbteFch>{submission.IssueDate:yyyyMMdd}</ar:CbteFch>
                    <ar:ImpTotal>{FormatDecimal(submission.Totals.TotalAmount)}</ar:ImpTotal>
                    <ar:ImpTotConc>{FormatDecimal(submission.Totals.NonTaxedAmount)}</ar:ImpTotConc>
                    <ar:ImpNeto>{FormatDecimal(submission.Totals.TaxableAmount)}</ar:ImpNeto>
                    <ar:ImpOpEx>{FormatDecimal(submission.Totals.ExemptAmount)}</ar:ImpOpEx>
                    <ar:ImpTrib>{FormatDecimal(submission.Totals.OtherTaxesAmount)}</ar:ImpTrib>
                    <ar:ImpIVA>{FormatDecimal(submission.Totals.VatAmount)}</ar:ImpIVA>
        """);

        if (submission.ServiceFrom is not null)
        {
            sb.Append($"<ar:FchServDesde>{submission.ServiceFrom:yyyyMMdd}</ar:FchServDesde>");
        }

        if (submission.ServiceTo is not null)
        {
            sb.Append($"<ar:FchServHasta>{submission.ServiceTo:yyyyMMdd}</ar:FchServHasta>");
        }

        if (submission.PaymentDueDate is not null)
        {
            sb.Append($"<ar:FchVtoPago>{submission.PaymentDueDate:yyyyMMdd}</ar:FchVtoPago>");
        }

        sb.Append($"""
                    <ar:MonId>{Escape(submission.Currency.Code)}</ar:MonId>
                    <ar:MonCotiz>{FormatDecimal(submission.Currency.ExchangeRate)}</ar:MonCotiz>
                    <ar:CondicionIVAReceptorId>{submission.Receiver.ReceiverVatConditionId}</ar:CondicionIVAReceptorId>
        """);

        if (submission.AssociatedVouchers.Count > 0)
        {
            sb.Append("<ar:CbtesAsoc>");
            foreach (var associated in submission.AssociatedVouchers)
            {
                sb.Append($"""
                    <ar:CbteAsoc>
                      <ar:Tipo>{associated.VoucherType.Code}</ar:Tipo>
                      <ar:PtoVta>{associated.PointOfSale}</ar:PtoVta>
                      <ar:Nro>{associated.VoucherNumber}</ar:Nro>
                """);

                if (associated.IssuerCuit is not null)
                {
                    sb.Append($"<ar:Cuit>{associated.IssuerCuit.Value}</ar:Cuit>");
                }

                if (associated.IssuedOn is not null)
                {
                    sb.Append($"<ar:CbteFch>{associated.IssuedOn:yyyyMMdd}</ar:CbteFch>");
                }

                sb.Append("</ar:CbteAsoc>");
            }
            sb.Append("</ar:CbtesAsoc>");
        }

        if (submission.TributeLines.Count > 0)
        {
            sb.Append("<ar:Tributos>");
            foreach (var tribute in submission.TributeLines)
            {
                sb.Append($"""
                    <ar:Tributo>
                      <ar:Id>{tribute.Id}</ar:Id>
                      <ar:Desc>{Escape(tribute.Description ?? string.Empty)}</ar:Desc>
                      <ar:BaseImp>{FormatDecimal(tribute.BaseAmount)}</ar:BaseImp>
                      <ar:Alic>{FormatDecimal(tribute.Rate)}</ar:Alic>
                      <ar:Importe>{FormatDecimal(tribute.Amount)}</ar:Importe>
                    </ar:Tributo>
                """);
            }
            sb.Append("</ar:Tributos>");
        }

        if (submission.VatLines.Count > 0)
        {
            sb.Append("<ar:Iva>");
            foreach (var vat in submission.VatLines)
            {
                sb.Append($"""
                    <ar:AlicIva>
                      <ar:Id>{vat.Id}</ar:Id>
                      <ar:BaseImp>{FormatDecimal(vat.BaseAmount)}</ar:BaseImp>
                      <ar:Importe>{FormatDecimal(vat.Amount)}</ar:Importe>
                    </ar:AlicIva>
                """);
            }
            sb.Append("</ar:Iva>");
        }

        sb.Append("""
                  </ar:FECAEDetRequest>
                </ar:FeDetReq>
              </ar:FeCAEReq>
            </ar:FECAESolicitar>
          </soapenv:Body>
        </soapenv:Envelope>
        """);

        return sb.ToString();
    }

    private static string BuildAuth(AccessTicket ticket, long issuerCuit) =>
        $"""
              <ar:Auth>
                <ar:Token>{Escape(ticket.Token)}</ar:Token>
                <ar:Sign>{Escape(ticket.Sign)}</ar:Sign>
                <ar:Cuit>{issuerCuit}</ar:Cuit>
              </ar:Auth>
        """;

    private (int DocType, long DocNumber) ResolveCustomerDocument(InternalInvoiceSubmission submission)
    {
        if (submission.Receiver.DocumentTypeCode is int docType &&
            long.TryParse(submission.Receiver.DocumentNumber, NumberStyles.None, CultureInfo.InvariantCulture, out var docNumber))
        {
            return (docType, docNumber);
        }

        if (submission.Receiver.IsConsumerFinal)
        {
            return (_options.ConsumerFinalDocumentTypeCode, _options.ConsumerFinalDocumentNumber);
        }

        throw new InvalidOperationException("Receiver document information is required to build a WSFEv1 submission.");
    }

    private static string FormatDecimal(decimal value) => value.ToString("0.00######", CultureInfo.InvariantCulture);

    private static string Escape(string value) => System.Security.SecurityElement.Escape(value) ?? string.Empty;
}
