using System.Globalization;
using System.Text;
using System.Text.Json;
using ARCANet.Abstractions;
using ARCANet.Invoices;
using QRCoder;

namespace ARCANet.Qr;

public sealed class ArcaQrGenerator : IArcaQrGenerator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = null,
        WriteIndented = false
    };

    private readonly ArcaQrOptions _options;

    public ArcaQrGenerator()
        : this(new ArcaQrOptions())
    {
    }

    public ArcaQrGenerator(ArcaQrOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));

        if (!_options.BaseUrl.IsAbsoluteUri)
        {
            throw new ArgumentException("QR base URL must be absolute.", nameof(options));
        }

        if (_options.Version <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "QR version must be greater than zero.");
        }
    }

    public ArcaQrPayload BuildPayload(AuthorizedInvoice invoice)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        return new ArcaQrPayload
        {
            Version = _options.Version,
            IssueDate = invoice.IssueDate,
            IssuerCuit = invoice.IssuerCuit,
            PointOfSale = invoice.Series.PointOfSale,
            VoucherTypeCode = invoice.Series.VoucherType.Code,
            VoucherNumber = invoice.VoucherNumber,
            TotalAmount = invoice.Totals.TotalAmount,
            CurrencyCode = invoice.Currency.Code,
            CurrencyExchangeRate = invoice.Currency.ExchangeRate,
            ReceiverDocumentTypeCode = invoice.Customer.DocumentTypeCode,
            ReceiverDocumentNumber = ParseOptionalLong(invoice.Customer.DocumentNumber),
            AuthorizationCodeType = MapAuthorizationCodeType(invoice.AuthorizationCodeType),
            AuthorizationCode = ParseRequiredLong(invoice.AuthorizationCode, nameof(invoice.AuthorizationCode))
        };
    }

    public string BuildJson(ArcaQrPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    public string BuildBase64(ArcaQrPayload payload)
    {
        var json = BuildJson(payload);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }

    public Uri BuildUrl(ArcaQrPayload payload)
    {
        var base64 = BuildBase64(payload);
        var separator = _options.BaseUrl.Query.Length == 0 ? "?" : "&";
        var url = string.Create(
            CultureInfo.InvariantCulture,
            $"{_options.BaseUrl}{separator}p={Uri.EscapeDataString(base64)}");

        return new Uri(url, UriKind.Absolute);
    }

    public string BuildSvg(AuthorizedInvoice invoice, int pixelsPerModule = 20) =>
        BuildSvg(BuildPayload(invoice), pixelsPerModule);

    public string BuildSvg(ArcaQrPayload payload, int pixelsPerModule = 20)
    {
        ValidatePixelsPerModule(pixelsPerModule);

        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(BuildUrl(payload).ToString(), QRCodeGenerator.ECCLevel.Q);
        var qr = new SvgQRCode(data);
        return qr.GetGraphic(pixelsPerModule);
    }

    public byte[] BuildPng(AuthorizedInvoice invoice, int pixelsPerModule = 20) =>
        BuildPng(BuildPayload(invoice), pixelsPerModule);

    public byte[] BuildPng(ArcaQrPayload payload, int pixelsPerModule = 20)
    {
        ValidatePixelsPerModule(pixelsPerModule);

        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(BuildUrl(payload).ToString(), QRCodeGenerator.ECCLevel.Q);
        var qr = new PngByteQRCode(data);
        return qr.GetGraphic(pixelsPerModule);
    }

    private static string MapAuthorizationCodeType(AuthorizationCodeType codeType) =>
        codeType switch
        {
            AuthorizationCodeType.Cae => "E",
            AuthorizationCodeType.Caea => "A",
            _ => throw new ArgumentOutOfRangeException(nameof(codeType), codeType, "Unknown authorization code type.")
        };

    private static long ParseRequiredLong(string rawValue, string paramName)
    {
        if (!long.TryParse(rawValue, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed))
        {
            throw new ArgumentException("Authorization code must be numeric for QR payload generation.", paramName);
        }

        return parsed;
    }

    private static long? ParseOptionalLong(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return null;
        }

        return long.Parse(rawValue, NumberStyles.None, CultureInfo.InvariantCulture);
    }

    private static void ValidatePixelsPerModule(int pixelsPerModule)
    {
        if (pixelsPerModule <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pixelsPerModule), "Pixels per module must be greater than zero.");
        }
    }
}
