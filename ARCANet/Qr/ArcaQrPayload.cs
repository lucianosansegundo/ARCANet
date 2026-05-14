using System.Text.Json.Serialization;

namespace ARCANet.Qr;

public sealed record ArcaQrPayload
{
    [JsonPropertyName("ver")]
    public required int Version { get; init; }

    [JsonPropertyName("fecha")]
    public required DateOnly IssueDate { get; init; }

    [JsonPropertyName("cuit")]
    public required long IssuerCuit { get; init; }

    [JsonPropertyName("ptoVta")]
    public required int PointOfSale { get; init; }

    [JsonPropertyName("tipoCmp")]
    public required int VoucherTypeCode { get; init; }

    [JsonPropertyName("nroCmp")]
    public required long VoucherNumber { get; init; }

    [JsonPropertyName("importe")]
    public required decimal TotalAmount { get; init; }

    [JsonPropertyName("moneda")]
    public required string CurrencyCode { get; init; }

    [JsonPropertyName("ctz")]
    public required decimal CurrencyExchangeRate { get; init; }

    [JsonPropertyName("tipoDocRec")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ReceiverDocumentTypeCode { get; init; }

    [JsonPropertyName("nroDocRec")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? ReceiverDocumentNumber { get; init; }

    [JsonPropertyName("tipoCodAut")]
    public required string AuthorizationCodeType { get; init; }

    [JsonPropertyName("codAut")]
    public required long AuthorizationCode { get; init; }
}
