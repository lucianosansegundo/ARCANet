using ARCANet.Invoices;
using ARCANet.Qr;
using ARCANet.Rendering;
using ARCANet.Rendering.Pdf;

var options = SampleOptions.Parse(args);
var model = SampleScenarioFactory.Create(options.Scenario);

Directory.CreateDirectory(Path.GetDirectoryName(options.OutputPath)!);

if (options.Format == OutputFormat.Html)
{
    var html = options.Layout switch
    {
        SampleLayout.A4 => new HtmlReceiptRenderer().RenderHtml(model),
        SampleLayout.Thermal58 => new ThermalReceiptHtmlRenderer().RenderHtml(model),
        SampleLayout.Thermal80 => new ThermalReceiptHtmlRenderer().RenderHtml(model),
        _ => throw new ArgumentOutOfRangeException()
    };

    await File.WriteAllTextAsync(options.OutputPath, html);
}
else
{
    var renderer = new ReceiptPdfRenderer();
    var pdf = renderer.RenderPdf(
        model,
        new ReceiptPdfRenderOptions
        {
            Layout = options.Layout switch
            {
                SampleLayout.A4 => ReceiptPdfPageLayout.A4,
                SampleLayout.Thermal58 => ReceiptPdfPageLayout.Thermal58Mm,
                SampleLayout.Thermal80 => ReceiptPdfPageLayout.Thermal80Mm,
                _ => throw new ArgumentOutOfRangeException()
            }
        });

    await File.WriteAllBytesAsync(options.OutputPath, pdf);
}

Console.WriteLine($"Archivo generado: {options.OutputPath}");
Console.WriteLine($"Formato: {options.Format}");
Console.WriteLine($"Layout: {options.Layout}");
Console.WriteLine($"Escenario: {options.Scenario}");

internal sealed record SampleOptions(OutputFormat Format, SampleLayout Layout, SampleScenario Scenario, string OutputPath)
{
    public static SampleOptions Parse(string[] args)
    {
        OutputFormat? format = null;
        var layout = SampleLayout.A4;
        var scenario = SampleScenario.ShortFacturaA;
        string? output = null;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--format":
                    format = ParseFormat(GetNext(args, ref i, "--format"));
                    break;
                case "--layout":
                    layout = ParseLayout(GetNext(args, ref i, "--layout"));
                    break;
                case "--scenario":
                    scenario = ParseScenario(GetNext(args, ref i, "--scenario"));
                    break;
                case "--output":
                    output = GetNext(args, ref i, "--output");
                    break;
                case "--help":
                case "-h":
                    PrintUsage();
                    Environment.Exit(0);
                    break;
                default:
                    throw new ArgumentException($"Parametro desconocido: {args[i]}");
            }
        }

        var resolvedFormat = format ?? (layout == SampleLayout.A4 ? OutputFormat.Pdf : OutputFormat.Html);
        output ??= BuildDefaultOutputPath(resolvedFormat, layout, scenario);
        return new SampleOptions(resolvedFormat, layout, scenario, Path.GetFullPath(output));
    }

    private static string GetNext(string[] args, ref int index, string optionName)
    {
        if (index + 1 >= args.Length)
        {
            throw new ArgumentException($"Falta valor para {optionName}");
        }

        index++;
        return args[index];
    }

    private static OutputFormat ParseFormat(string raw) =>
        raw.ToLowerInvariant() switch
        {
            "html" => OutputFormat.Html,
            "pdf" => OutputFormat.Pdf,
            _ => throw new ArgumentException($"Formato no soportado: {raw}")
        };

    private static SampleLayout ParseLayout(string raw) =>
        raw.ToLowerInvariant() switch
        {
            "a4" => SampleLayout.A4,
            "thermal58" => SampleLayout.Thermal58,
            "thermal80" => SampleLayout.Thermal80,
            _ => throw new ArgumentException($"Layout no soportado: {raw}")
        };

    private static SampleScenario ParseScenario(string raw) =>
        raw.ToLowerInvariant() switch
        {
            "short-factura-a" => SampleScenario.ShortFacturaA,
            "long-factura-b" => SampleScenario.LongFacturaB,
            "credit-note-b" => SampleScenario.CreditNoteB,
            _ => throw new ArgumentException($"Escenario no soportado: {raw}")
        };

    private static string BuildDefaultOutputPath(OutputFormat format, SampleLayout layout, SampleScenario scenario)
    {
        var extension = format == OutputFormat.Html ? "html" : "pdf";
        var fileName = $"{scenario.ToString().ToLowerInvariant()}-{layout.ToString().ToLowerInvariant()}.{extension}";
        return Path.Combine(Environment.CurrentDirectory, "sample-output", fileName);
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Uso:");
        Console.WriteLine("  dotnet run --project samples/ARCANet.Rendering.Sample -- --layout thermal58 --scenario long-factura-b --output C:\\tmp\\ticket.html");
        Console.WriteLine("  dotnet run --project samples/ARCANet.Rendering.Sample -- --layout a4 --scenario short-factura-a --output C:\\tmp\\factura.pdf");
        Console.WriteLine();
        Console.WriteLine("Opciones:");
        Console.WriteLine("  --format    html | pdf (opcional; default: pdf para a4, html para thermal58/thermal80)");
        Console.WriteLine("  --layout    a4 | thermal58 | thermal80");
        Console.WriteLine("  --scenario  short-factura-a | long-factura-b | credit-note-b");
        Console.WriteLine("  --output    ruta destino opcional");
    }
}

internal static class SampleScenarioFactory
{
    private static readonly ArcaQrGenerator QrGenerator = new();

    public static ReceiptRenderModel Create(SampleScenario scenario) =>
        scenario switch
        {
            SampleScenario.ShortFacturaA => CreateShortFacturaA(),
            SampleScenario.LongFacturaB => CreateLongFacturaB(),
            SampleScenario.CreditNoteB => CreateCreditNoteB(),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null)
        };

    private static ReceiptRenderModel CreateShortFacturaA()
    {
        var invoice = WithQr(
            new AuthorizedInvoice
            {
                IssuerCuit = 20304050607,
                Series = new VoucherSeries(
                    20304050607,
                    5,
                    new VoucherType(1, "Factura A")),
                VoucherNumber = 1234,
                IssueDate = new DateOnly(2026, 5, 20),
                Concept = InvoiceConcept.Products,
                Customer = new CustomerIdentity
                {
                    Name = "Cliente SA",
                    DocumentTypeCode = 80,
                    DocumentNumber = "30712345678"
                },
                ReceiverVatCondition = new ReceiverVatCondition(1, "IVA Responsable Inscripto"),
                Totals = new MoneyTotals
                {
                    TotalAmount = 1210m,
                    TaxableAmount = 1000m,
                    VatAmount = 210m
                },
                Currency = new CurrencyAmount("PES", 1m),
                VatItems =
                [
                    new VatItem
                    {
                        Id = 5,
                        BaseAmount = 1000m,
                        Rate = 21m,
                        Amount = 210m
                    }
                ],
                AuthorizationCodeType = AuthorizationCodeType.Cae,
                AuthorizationCode = "12345678901234",
                AuthorizationDueDate = new DateOnly(2026, 6, 1),
                ProcessedAtUtc = new DateTimeOffset(2026, 5, 20, 13, 0, 0, TimeSpan.Zero)
            });

        return new ReceiptRenderModel
        {
            Invoice = invoice,
            Issuer = new IssuerDisplayInfo
            {
                DisplayName = "Comercio Demo S.A.",
                TaxId = "30-71234567-8",
                VatConditionLabel = "IVA Responsable Inscripto",
                Address = "Av. Siempre Viva 123, CABA",
                GrossIncomeNumber = "902-123456-7",
                BusinessStartDate = new DateOnly(2020, 1, 15)
            },
            Items =
            [
                new ReceiptLineItem
                {
                    Description = "Aceite 2L",
                    Quantity = 2,
                    UnitPrice = 500m,
                    Subtotal = 1000m
                }
            ],
            PaymentDescription = "Tarjeta de debito",
            CashierName = "Caja 1",
            FooterText = "Gracias por su compra."
        };
    }

    private static ReceiptRenderModel CreateLongFacturaB()
    {
        var invoice = WithQr(
            new AuthorizedInvoice
            {
                IssuerCuit = 20304050607,
                Series = new VoucherSeries(
                    20304050607,
                    8,
                    new VoucherType(6, "Factura B")),
                VoucherNumber = 84521,
                IssueDate = new DateOnly(2026, 5, 20),
                Concept = InvoiceConcept.ProductsAndServices,
                ServiceFrom = new DateOnly(2026, 5, 1),
                ServiceTo = new DateOnly(2026, 5, 31),
                PaymentDueDate = new DateOnly(2026, 6, 5),
                Customer = new CustomerIdentity
                {
                    Name = "Consumidor Final con nombre particularmente largo para validar corte de linea",
                    IsConsumerFinal = true
                },
                ReceiverVatCondition = new ReceiverVatCondition(5, "Consumidor Final"),
                Totals = new MoneyTotals
                {
                    TotalAmount = 98765.43m,
                    TaxableAmount = 81623.50m,
                    VatAmount = 17141.93m
                },
                Currency = new CurrencyAmount("PES", 1m),
                AuthorizationCodeType = AuthorizationCodeType.Cae,
                AuthorizationCode = "12345678901235",
                AuthorizationDueDate = new DateOnly(2026, 6, 2),
                ProcessedAtUtc = new DateTimeOffset(2026, 5, 20, 15, 30, 0, TimeSpan.Zero)
            });

        return new ReceiptRenderModel
        {
            Invoice = invoice,
            Issuer = new IssuerDisplayInfo
            {
                DisplayName = "Sucursal Centro Comercial del Litoral SRL",
                TaxId = "30-70123456-1",
                VatConditionLabel = "IVA Responsable Inscripto",
                Address = "Boulevard Comercial 456, Rosario, Santa Fe"
            },
            Items =
            [
                new ReceiptLineItem
                {
                    Description = "Servicio de mantenimiento preventivo mensual con descripcion extensa para validar wrapping",
                    Quantity = 1,
                    UnitPrice = 25000m,
                    DiscountAmount = 1500m,
                    Subtotal = 23500m
                },
                new ReceiptLineItem
                {
                    Description = "Repuesto tecnico modelo ZX-2000",
                    Quantity = 3,
                    UnitPrice = 12000m,
                    Subtotal = 36000m
                },
                new ReceiptLineItem
                {
                    Description = "Insumos varios",
                    Quantity = 12,
                    UnitPrice = 1843.625m,
                    Subtotal = 22123.50m
                }
            ],
            PaymentDescription = "Transferencia bancaria",
            CashierName = "Caja Central",
            FooterText = "Comprobante de muestra para validacion visual de textos largos."
        };
    }

    private static ReceiptRenderModel CreateCreditNoteB()
    {
        var invoice = WithQr(
            new AuthorizedInvoice
            {
                IssuerCuit = 20304050607,
                Series = new VoucherSeries(
                    20304050607,
                    5,
                    new VoucherType(8, "Nota de Credito B", VoucherKind.CreditNote)),
                VoucherNumber = 102,
                IssueDate = new DateOnly(2026, 5, 20),
                Concept = InvoiceConcept.Products,
                Customer = new CustomerIdentity
                {
                    Name = "Consumidor Final",
                    IsConsumerFinal = true
                },
                ReceiverVatCondition = new ReceiverVatCondition(5, "Consumidor Final"),
                Totals = new MoneyTotals
                {
                    TotalAmount = 5000m,
                    TaxableAmount = 5000m
                },
                Currency = new CurrencyAmount("PES", 1m),
                AssociatedVouchers =
                [
                    new AssociatedVoucher
                    {
                        VoucherType = new VoucherType(6, "Factura B"),
                        PointOfSale = 5,
                        VoucherNumber = 84521,
                        IssuedOn = new DateOnly(2026, 5, 20)
                    }
                ],
                AuthorizationCodeType = AuthorizationCodeType.Cae,
                AuthorizationCode = "12345678901236",
                AuthorizationDueDate = new DateOnly(2026, 6, 3),
                ProcessedAtUtc = new DateTimeOffset(2026, 5, 20, 17, 10, 0, TimeSpan.Zero)
            });

        return new ReceiptRenderModel
        {
            Invoice = invoice,
            Issuer = new IssuerDisplayInfo
            {
                DisplayName = "Comercio Demo S.A.",
                TaxId = "30-71234567-8",
                VatConditionLabel = "IVA Responsable Inscripto",
                Address = "Av. Siempre Viva 123, CABA"
            },
            Items =
            [
                new ReceiptLineItem
                {
                    Description = "Anulacion parcial de venta",
                    Quantity = 1,
                    UnitPrice = 5000m,
                    Subtotal = 5000m
                }
            ],
            FooterText = "Muestra de nota de credito con comprobante asociado."
        };
    }

    private static AuthorizedInvoice WithQr(AuthorizedInvoice invoice)
    {
        var payload = QrGenerator.BuildPayload(invoice);
        return invoice with
        {
            QrPayload = payload,
            QrUrl = QrGenerator.BuildUrl(payload)
        };
    }
}

internal enum OutputFormat
{
    Html,
    Pdf
}

internal enum SampleLayout
{
    A4,
    Thermal58,
    Thermal80
}

internal enum SampleScenario
{
    ShortFacturaA,
    LongFacturaB,
    CreditNoteB
}
