using System.Globalization;
using System.Xml.Linq;
using ARCANet.Invoices;

namespace ARCANet.Wsfev1;

internal sealed class WsfeSoapResponseParser
{
    public long? ParseLastAuthorizedNumber(string soapResponse)
    {
        var document = XDocument.Parse(soapResponse);
        ThrowIfSoapFault(document, "WSFEv1 last authorized number");
        var cbteNro = document.Descendants().FirstOrDefault(x => x.Name.LocalName == "CbteNro")?.Value;

        if (long.TryParse(cbteNro, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return null;
    }

    public WsfeInvoiceRecord? ParseCompConsultar(string soapResponse)
    {
        var document = XDocument.Parse(soapResponse);
        ThrowIfSoapFault(document, "WSFEv1 invoice lookup");
        var errors = ParseIssues(document, "Err");

        if (errors.Any(x => x.Code == "602"))
        {
            return null;
        }

        var resultGet = document.Descendants().FirstOrDefault(x => x.Name.LocalName == "ResultGet");
        if (resultGet is null)
        {
            return null;
        }

        var result = GetRequiredValue(resultGet, "Resultado");
        var cae = GetRequiredValue(resultGet, "CodAutorizacion");
        var emissionType = GetOptionalValue(resultGet, "EmisionTipo") ?? "CAE";
        var issueDate = ParseDateOnly(GetRequiredValue(resultGet, "CbteFch"));
        var caeDueDate = ParseDateOnly(GetRequiredValue(resultGet, "FchVto"));
        var processedAt = ParseDateTimeOffset(GetRequiredValue(resultGet, "FchProceso"));

        return new WsfeInvoiceRecord
        {
            IssuerCuit = ParseLong(GetRequiredValue(resultGet, "Cuit")),
            PointOfSale = ParseInt(GetRequiredValue(resultGet, "PtoVta")),
            VoucherTypeCode = ParseInt(GetRequiredValue(resultGet, "CbteTipo")),
            VoucherNumber = ParseLong(GetRequiredValue(resultGet, "CbteDesde")),
            Result = result,
            AuthorizationCode = cae,
            IssueDate = issueDate,
            Concept = (InvoiceConcept)ParseInt(GetRequiredValue(resultGet, "Concepto")),
            CustomerDocumentTypeCode = ParseInt(GetRequiredValue(resultGet, "DocTipo")),
            CustomerDocumentNumber = GetRequiredValue(resultGet, "DocNro"),
            ServiceFrom = ParseOptionalDateOnly(GetOptionalValue(resultGet, "FchServDesde")),
            ServiceTo = ParseOptionalDateOnly(GetOptionalValue(resultGet, "FchServHasta")),
            PaymentDueDate = ParseOptionalDateOnly(GetOptionalValue(resultGet, "FchVtoPago")),
            Totals = new MoneyTotals
            {
                TotalAmount = ParseDecimal(GetRequiredValue(resultGet, "ImpTotal")),
                NonTaxedAmount = ParseDecimal(GetRequiredValue(resultGet, "ImpTotConc")),
                TaxableAmount = ParseDecimal(GetRequiredValue(resultGet, "ImpNeto")),
                ExemptAmount = ParseDecimal(GetRequiredValue(resultGet, "ImpOpEx")),
                OtherTaxesAmount = ParseDecimal(GetRequiredValue(resultGet, "ImpTrib")),
                VatAmount = ParseDecimal(GetRequiredValue(resultGet, "ImpIVA"))
            },
            Currency = new CurrencyAmount(
                GetRequiredValue(resultGet, "MonId"),
                ParseDecimal(GetRequiredValue(resultGet, "MonCotiz"))),
            VatItems = ParseVatItems(resultGet),
            Tributes = ParseTributes(resultGet),
            AssociatedVouchers = ParseAssociatedVouchers(resultGet),
            AuthorizationDueDate = caeDueDate,
            ProcessedAtUtc = processedAt,
            EmissionType = emissionType,
            Observations = ParseWrappedIssues(resultGet, "Observaciones", "Obs"),
            Events = ParseIssues(document, "Evt"),
            Errors = errors
        };
    }

    public WsfeAuthorizationResponse ParseFeCaeSolicitar(string soapResponse)
    {
        var document = XDocument.Parse(soapResponse);
        ThrowIfSoapFault(document, "WSFEv1 authorization");
        var header = document.Descendants().FirstOrDefault(x => x.Name.LocalName == "FeCabResp");
        var detail = document.Descendants().FirstOrDefault(x =>
            x.Name.LocalName is "FECAEDetResponse" or "FEDetResponse");

        if (header is null)
        {
            throw new InvalidOperationException("WSFEv1 authorization response does not contain FeCabResp.");
        }

        var headerResult = GetRequiredValue(header, "Resultado");
        var reprocess = GetOptionalValue(header, "Reproceso") ?? "N";
        var processedAt = ParseDateTimeOffset(GetRequiredValue(header, "FchProceso"));

        var detailResult = detail is null ? headerResult : GetOptionalValue(detail, "Resultado") ?? headerResult;
        var authorizationCode = detail is null ? null : GetOptionalValue(detail, "CAE");
        var authorizationDueDateText = detail is null ? null : GetOptionalValue(detail, "CAEFchVto");

        return new WsfeAuthorizationResponse
        {
            HeaderResult = headerResult,
            DetailResult = detailResult,
            Reprocess = reprocess,
            ProcessedAtUtc = processedAt,
            AuthorizationCode = authorizationCode,
            AuthorizationDueDate = authorizationDueDateText is null ? null : ParseDateOnly(authorizationDueDateText),
            Errors = ParseIssues(document, "Err"),
            Events = ParseIssues(document, "Evt"),
            Observations = detail is null ? [] : ParseWrappedIssues(detail, "Observaciones", "Obs")
        };
    }

    private static IReadOnlyList<WsfeResultIssue> ParseIssues(XContainer node, string issueNodeName) =>
        node
            .Descendants()
            .Where(x => x.Name.LocalName == issueNodeName)
            .Select(x => new WsfeResultIssue(
                GetOptionalValue(x, "Code") ?? string.Empty,
                GetOptionalValue(x, "Msg") ?? string.Empty))
            .Where(x => !string.IsNullOrWhiteSpace(x.Code) || !string.IsNullOrWhiteSpace(x.Message))
            .ToArray();

    private static IReadOnlyList<WsfeResultIssue> ParseWrappedIssues(
        XContainer node,
        string containerName,
        string issueNodeName) =>
        node
            .Descendants()
            .Where(x => x.Name.LocalName == containerName)
            .Elements()
            .Where(x => x.Name.LocalName == issueNodeName)
            .Select(x => new WsfeResultIssue(
                GetOptionalValue(x, "Code") ?? string.Empty,
                GetOptionalValue(x, "Msg") ?? string.Empty))
            .Where(x => !string.IsNullOrWhiteSpace(x.Code) || !string.IsNullOrWhiteSpace(x.Message))
            .ToArray();

    private static string GetRequiredValue(XContainer parent, string childName) =>
        GetOptionalValue(parent, childName) ?? throw new InvalidOperationException($"Missing required field '{childName}' in WSFEv1 response.");

    private static string? GetOptionalValue(XContainer parent, string childName) =>
        parent.Elements().FirstOrDefault(x => x.Name.LocalName == childName)?.Value;

    private static int ParseInt(string value) => int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);

    private static long ParseLong(string value) => long.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);

    private static decimal ParseDecimal(string value) => decimal.Parse(value, NumberStyles.Number, CultureInfo.InvariantCulture);

    private static DateOnly ParseDateOnly(string value)
    {
        if (DateOnly.TryParseExact(value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateOnly))
        {
            return dateOnly;
        }

        return DateOnly.Parse(value, CultureInfo.InvariantCulture);
    }

    private static DateOnly? ParseOptionalDateOnly(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : ParseDateOnly(value);

    private static DateTimeOffset ParseDateTimeOffset(string value)
    {
        if (DateTimeOffset.TryParseExact(value, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var compact))
        {
            return compact;
        }

        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
        {
            return parsed;
        }

        return DateTimeOffset.ParseExact(value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
    }

    private static IReadOnlyList<VatItem> ParseVatItems(XContainer resultGet) =>
        resultGet
            .Descendants()
            .Where(x => x.Name.LocalName == "AlicIva")
            .Select(x => new VatItem
            {
                Id = ParseInt(GetRequiredValue(x, "Id")),
                BaseAmount = ParseDecimal(GetRequiredValue(x, "BaseImp")),
                Amount = ParseDecimal(GetRequiredValue(x, "Importe")),
                Rate = 0m
            })
            .ToArray();

    private static IReadOnlyList<TributeItem> ParseTributes(XContainer resultGet) =>
        resultGet
            .Descendants()
            .Where(x => x.Name.LocalName == "Tributo")
            .Select(x => new TributeItem
            {
                Id = ParseInt(GetRequiredValue(x, "Id")),
                Description = GetOptionalValue(x, "Desc"),
                BaseAmount = ParseDecimal(GetRequiredValue(x, "BaseImp")),
                Rate = ParseDecimal(GetRequiredValue(x, "Alic")),
                Amount = ParseDecimal(GetRequiredValue(x, "Importe"))
            })
            .ToArray();

    private static IReadOnlyList<AssociatedVoucher> ParseAssociatedVouchers(XContainer resultGet) =>
        resultGet
            .Descendants()
            .Where(x => x.Name.LocalName == "CbteAsoc")
            .Select(x => new AssociatedVoucher
            {
                VoucherType = new VoucherType(
                    ParseInt(GetRequiredValue(x, "Tipo")),
                    $"Cbte {GetRequiredValue(x, "Tipo")}"),
                PointOfSale = ParseInt(GetRequiredValue(x, "PtoVta")),
                VoucherNumber = ParseLong(GetRequiredValue(x, "Nro")),
                IssuerCuit = GetOptionalValue(x, "Cuit") is string cuit ? ParseLong(cuit) : null,
                IssuedOn = ParseOptionalDateOnly(GetOptionalValue(x, "CbteFch"))
            })
            .ToArray();

    private static void ThrowIfSoapFault(XDocument document, string operationName)
    {
        var fault = document.Descendants().FirstOrDefault(x => x.Name.LocalName == "Fault");
        if (fault is null)
        {
            return;
        }

        var faultCode = fault.Descendants().FirstOrDefault(x => x.Name.LocalName == "faultcode" || x.Name.LocalName == "Code")?.Value;
        var faultString = fault.Descendants().FirstOrDefault(x => x.Name.LocalName == "faultstring" || x.Name.LocalName == "Reason")?.Value;

        var message = string.IsNullOrWhiteSpace(faultCode) && string.IsNullOrWhiteSpace(faultString)
            ? $"{operationName} returned a SOAP fault."
            : $"{operationName} returned a SOAP fault. Code: {faultCode ?? "(none)"}. Message: {faultString ?? "(none)"}";

        throw new InvalidOperationException(message);
    }
}
