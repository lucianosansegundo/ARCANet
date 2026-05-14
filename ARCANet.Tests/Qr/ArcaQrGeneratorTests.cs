using System.Text;
using System.Text.Json;
using ARCANet.Invoices;
using ARCANet.Qr;

namespace ARCANet.Tests.Qr;

public sealed class ArcaQrGeneratorTests
{
    [Fact]
    public void BuildPayload_MapsAuthorizedInvoiceToOfficialFields()
    {
        var generator = new ArcaQrGenerator();
        var invoice = CreateAuthorizedInvoice();

        var payload = generator.BuildPayload(invoice);

        Assert.Equal(1, payload.Version);
        Assert.Equal(new DateOnly(2026, 5, 14), payload.IssueDate);
        Assert.Equal(20304050607, payload.IssuerCuit);
        Assert.Equal(5, payload.PointOfSale);
        Assert.Equal(1, payload.VoucherTypeCode);
        Assert.Equal(1234, payload.VoucherNumber);
        Assert.Equal(1210.00m, payload.TotalAmount);
        Assert.Equal("PES", payload.CurrencyCode);
        Assert.Equal(1.00m, payload.CurrencyExchangeRate);
        Assert.Equal(96, payload.ReceiverDocumentTypeCode);
        Assert.Equal(30111222, payload.ReceiverDocumentNumber);
        Assert.Equal("E", payload.AuthorizationCodeType);
        Assert.Equal(70417054367476, payload.AuthorizationCode);
    }

    [Fact]
    public void BuildJson_UsesOfficialFieldNamesAndOmitsMissingReceiverDocument()
    {
        var generator = new ArcaQrGenerator();
        var invoice = CreateAuthorizedInvoice() with
        {
            Customer = new CustomerIdentity
            {
                Name = "Consumidor Final",
                IsConsumerFinal = true
            }
        };

        var payload = generator.BuildPayload(invoice);
        var json = generator.BuildJson(payload);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.Equal(1, root.GetProperty("ver").GetInt32());
        Assert.Equal("2026-05-14", root.GetProperty("fecha").GetString());
        Assert.Equal(20304050607, root.GetProperty("cuit").GetInt64());
        Assert.Equal(5, root.GetProperty("ptoVta").GetInt32());
        Assert.Equal(1, root.GetProperty("tipoCmp").GetInt32());
        Assert.Equal(1234, root.GetProperty("nroCmp").GetInt64());
        Assert.Equal(1210.00m, root.GetProperty("importe").GetDecimal());
        Assert.Equal("PES", root.GetProperty("moneda").GetString());
        Assert.Equal(1.00m, root.GetProperty("ctz").GetDecimal());
        Assert.Equal("E", root.GetProperty("tipoCodAut").GetString());
        Assert.Equal(70417054367476, root.GetProperty("codAut").GetInt64());
        Assert.False(root.TryGetProperty("tipoDocRec", out _));
        Assert.False(root.TryGetProperty("nroDocRec", out _));
    }

    [Fact]
    public void BuildBase64_EncodesJsonPayloadAsUtf8()
    {
        var generator = new ArcaQrGenerator();
        var payload = generator.BuildPayload(CreateAuthorizedInvoice());

        var base64 = generator.BuildBase64(payload);
        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(base64));

        Assert.Equal(generator.BuildJson(payload), decoded);
    }

    [Fact]
    public void BuildUrl_UsesConfiguredBaseUrlAndEscapedPayload()
    {
        var generator = new ArcaQrGenerator(new ArcaQrOptions
        {
            BaseUrl = new Uri("https://example.test/qr"),
            Version = 7
        });

        var payload = generator.BuildPayload(CreateAuthorizedInvoice());
        var url = generator.BuildUrl(payload);
        var expectedBase64 = generator.BuildBase64(payload);

        Assert.StartsWith("https://example.test/qr?p=", url.ToString(), StringComparison.Ordinal);
        Assert.Equal($"?p={Uri.EscapeDataString(expectedBase64)}", url.Query);
        Assert.Equal(7, payload.Version);
    }

    private static AuthorizedInvoice CreateAuthorizedInvoice() =>
        new()
        {
            IssuerCuit = 20304050607,
            Series = new VoucherSeries(20304050607, 5, new VoucherType(1, "Factura A")),
            VoucherNumber = 1234,
            IssueDate = new DateOnly(2026, 5, 14),
            Concept = InvoiceConcept.Products,
            Customer = new CustomerIdentity
            {
                Name = "Cliente SA",
                DocumentTypeCode = 96,
                DocumentNumber = "30111222"
            },
            ReceiverVatCondition = new ReceiverVatCondition(1, "IVA Responsable Inscripto"),
            Totals = new MoneyTotals
            {
                TotalAmount = 1210.00m,
                TaxableAmount = 1000.00m,
                VatAmount = 210.00m
            },
            Currency = new CurrencyAmount("PES", 1.00m),
            AuthorizationCodeType = AuthorizationCodeType.Cae,
            AuthorizationCode = "70417054367476",
            AuthorizationDueDate = new DateOnly(2026, 5, 24),
            ProcessedAtUtc = new DateTimeOffset(2026, 5, 14, 12, 0, 0, TimeSpan.Zero)
        };
}
