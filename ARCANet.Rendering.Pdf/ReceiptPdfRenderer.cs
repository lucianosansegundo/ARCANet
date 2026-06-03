using ARCANet.Invoices;
using ARCANet.Rendering.Pdf.Internal;
using ARCANet.Qr;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Fonts;
using QRCoder;

namespace ARCANet.Rendering.Pdf;

public sealed class ReceiptPdfRenderer : IReceiptPdfRenderer
{
    private readonly ArcaQrGenerator _qrGenerator = new();

    private static readonly object FontResolverLock = new();
    private static bool _fontResolverConfigured;

    static ReceiptPdfRenderer()
    {
        EnsureFontResolverConfigured();
    }

    public byte[] RenderPdf(ReceiptRenderModel model, ReceiptPdfRenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(model);

        using var stream = new MemoryStream();
        RenderPdf(model, stream, options);
        return stream.ToArray();
    }

    public void RenderPdf(ReceiptRenderModel model, Stream output, ReceiptPdfRenderOptions? options = null)
    {
        EnsureFontResolverConfigured();

        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(model.Invoice);
        ArgumentNullException.ThrowIfNull(model.Issuer);

        using var qrAssets = CreateQrAssets(model.Invoice, _qrGenerator);

        var document = CreateDocument(model, options ?? new ReceiptPdfRenderOptions(), qrAssets);
        var renderer = new PdfDocumentRenderer
        {
            Document = document
        };

        renderer.RenderDocument();
        var pdfDocument = renderer.PdfDocument ?? throw new InvalidOperationException("PDFsharp did not produce a PDF document.");
        pdfDocument.Info.Title = $"{model.Invoice.Series.VoucherType.Name} {PdfReceiptFormatting.FormatVoucherNumber(model.Invoice)}";
        pdfDocument.Info.Subject = "Comprobante fiscal renderizado";
        pdfDocument.Info.Author = "ARCANet.Rendering.Pdf";
        pdfDocument.Info.Creator = "ARCANet.Rendering.Pdf";
        pdfDocument.Save(output, closeStream: false);
    }

    private static Document CreateDocument(ReceiptRenderModel model, ReceiptPdfRenderOptions options, QrRenderAssets qrAssets)
    {
        var document = new Document();
        DefineStyles(document);

        var section = document.AddSection();
        ConfigurePage(section, options.Layout);

        if (options.Layout == ReceiptPdfPageLayout.A4)
        {
            ComposeA4(section, model, qrAssets);
        }
        else
        {
            ComposeThermal(section, model, options.Layout, qrAssets);
        }

        return document;
    }

    private static void EnsureFontResolverConfigured()
    {
        if (_fontResolverConfigured)
        {
            return;
        }

        lock (FontResolverLock)
        {
            if (_fontResolverConfigured)
            {
                return;
            }

            if (GlobalFontSettings.FontResolver is null)
            {
                GlobalFontSettings.FontResolver = new EmbeddedFontResolver();
            }

            _fontResolverConfigured = true;
        }
    }

    private static void DefineStyles(Document document)
    {
        var normal = document.Styles["Normal"] ?? throw new InvalidOperationException("MigraDoc default style 'Normal' was not found.");
        normal.Font.Name = "Arial";
        normal.Font.Size = 9;

        var title = document.Styles.AddStyle("ReceiptTitle", "Normal");
        title.Font.Size = 15;
        title.Font.Bold = true;

        var sectionTitle = document.Styles.AddStyle("ReceiptSectionTitle", "Normal");
        sectionTitle.Font.Size = 10;
        sectionTitle.Font.Bold = true;
        sectionTitle.ParagraphFormat.SpaceAfter = Unit.FromMillimeter(2);

        var small = document.Styles.AddStyle("ReceiptSmall", "Normal");
        small.Font.Size = 8;

        var thermalTitle = document.Styles.AddStyle("ThermalTitle", "Normal");
        thermalTitle.Font.Size = 11;
        thermalTitle.Font.Bold = true;
        thermalTitle.ParagraphFormat.Alignment = ParagraphAlignment.Center;
    }

    private static void ConfigurePage(Section section, ReceiptPdfPageLayout layout)
    {
        section.PageSetup.TopMargin = Unit.FromMillimeter(12);
        section.PageSetup.BottomMargin = Unit.FromMillimeter(12);
        section.PageSetup.LeftMargin = Unit.FromMillimeter(12);
        section.PageSetup.RightMargin = Unit.FromMillimeter(12);

        if (layout == ReceiptPdfPageLayout.A4)
        {
            section.PageSetup.PageFormat = PageFormat.A4;
            return;
        }

        section.PageSetup.PageWidth = Unit.FromMillimeter(layout == ReceiptPdfPageLayout.Thermal58Mm ? 58 : 80);
        section.PageSetup.PageHeight = Unit.FromMillimeter(297);
        section.PageSetup.TopMargin = Unit.FromMillimeter(5);
        section.PageSetup.BottomMargin = Unit.FromMillimeter(5);
        section.PageSetup.LeftMargin = Unit.FromMillimeter(4);
        section.PageSetup.RightMargin = Unit.FromMillimeter(4);
    }

    private static void ComposeA4(Section section, ReceiptRenderModel model, QrRenderAssets qrAssets)
    {
        ComposeA4Header(section, model);
        AddSpacer(section, 4);
        ComposePartiesTable(section, model);
        ComposeServicePeriod(section, model.Invoice);
        ComposeItemsTable(section, model.Items);
        ComposeTotals(section, model.Invoice);
        ComposeConsumerFiscalTransparency(section, model.Invoice);
        ComposeAssociatedVouchers(section, model.Invoice);
        ComposeOperationalDetails(section, model);
        ComposeAuthorization(section, model.Invoice);
        ComposeQrReference(section, qrAssets);
        ComposeFooterText(section, model.FooterText);
    }

    private static void ComposeA4Header(Section section, ReceiptRenderModel model)
    {
        var invoice = model.Invoice;
        var table = section.AddTable();
        table.Borders.Visible = false;
        table.AddColumn(Unit.FromCentimeter(10.5));
        table.AddColumn(Unit.FromCentimeter(7.0));

        var row = table.AddRow();
        var left = row.Cells[0];
        left.AddParagraph(model.Issuer.DisplayName).Style = "ReceiptTitle";
        AddOptionalParagraph(left, $"CUIT {model.Issuer.TaxId}");
        AddOptionalParagraph(left, model.Issuer.VatConditionLabel);
        AddOptionalParagraph(left, model.Issuer.Address);
        AddOptionalParagraph(left, model.Issuer.GrossIncomeNumber is null ? null : $"Ingresos Brutos: {model.Issuer.GrossIncomeNumber}");
        AddOptionalParagraph(left, model.Issuer.BusinessStartDate is null ? null : $"Inicio de actividades: {PdfReceiptFormatting.FormatDate(model.Issuer.BusinessStartDate.Value)}");

        var right = row.Cells[1];
        right.Borders.Width = 0.75;
        right.Borders.Color = Colors.Gray;
        right.Shading.Color = Colors.LightGray;
        right.VerticalAlignment = VerticalAlignment.Center;
        right.AddParagraph(invoice.Series.VoucherType.Name).Style = "ReceiptTitle";
        AddOptionalParagraph(right, $"Punto de venta y numero: {PdfReceiptFormatting.FormatVoucherNumber(invoice)}");
        AddOptionalParagraph(right, $"Fecha de emision: {PdfReceiptFormatting.FormatDate(invoice.IssueDate)}");
        AddOptionalParagraph(right, $"Concepto: {PdfReceiptFormatting.FormatConcept(invoice.Concept)}");
    }

    private static void ComposePartiesTable(Section section, ReceiptRenderModel model)
    {
        AddSectionTitle(section, "Emisor y receptor");

        var invoice = model.Invoice;
        var table = section.AddTable();
        table.Borders.Width = 0.5;
        table.AddColumn(Unit.FromCentimeter(8.75));
        table.AddColumn(Unit.FromCentimeter(8.75));

        var row = table.AddRow();
        FillCell(row.Cells[0], "Emisor", [
            $"Razon social: {model.Issuer.DisplayName}",
            $"CUIT: {model.Issuer.TaxId}",
            $"Condicion IVA: {model.Issuer.VatConditionLabel}"
        ]);

        FillCell(row.Cells[1], "Receptor", [
            $"Nombre: {invoice.Customer.Name}",
            $"Documento: {PdfReceiptFormatting.FormatCustomerDocument(invoice.Customer)}",
            $"Condicion IVA: {invoice.ReceiverVatCondition.Name}"
        ]);

        AddSpacer(section, 3);
    }

    private static void ComposeServicePeriod(Section section, AuthorizedInvoice invoice)
    {
        if (invoice.ServiceFrom is null && invoice.ServiceTo is null && invoice.PaymentDueDate is null)
        {
            return;
        }

        AddSectionTitle(section, "Periodo de servicio");
        AddOptionalParagraph(section, invoice.ServiceFrom is null ? null : $"Desde: {PdfReceiptFormatting.FormatDate(invoice.ServiceFrom.Value)}");
        AddOptionalParagraph(section, invoice.ServiceTo is null ? null : $"Hasta: {PdfReceiptFormatting.FormatDate(invoice.ServiceTo.Value)}");
        AddOptionalParagraph(section, invoice.PaymentDueDate is null ? null : $"Vencimiento de pago: {PdfReceiptFormatting.FormatDate(invoice.PaymentDueDate.Value)}");
        AddSpacer(section, 3);
    }

    private static void ComposeItemsTable(Section section, IReadOnlyList<ReceiptLineItem> items)
    {
        if (items.Count == 0)
        {
            return;
        }

        AddSectionTitle(section, "Detalle comercial");

        var table = section.AddTable();
        table.Borders.Width = 0.5;
        table.Rows.LeftIndent = 0;
        table.AddColumn(Unit.FromCentimeter(7.2));
        table.AddColumn(Unit.FromCentimeter(2.0));
        table.AddColumn(Unit.FromCentimeter(2.6));
        table.AddColumn(Unit.FromCentimeter(2.6));
        table.AddColumn(Unit.FromCentimeter(2.9));

        var header = table.AddRow();
        header.Shading.Color = Colors.LightGray;
        WriteCell(header.Cells[0], "Descripcion", true);
        WriteCell(header.Cells[1], "Cant.", true);
        WriteCell(header.Cells[2], "P. unit.", true);
        WriteCell(header.Cells[3], "Desc.", true);
        WriteCell(header.Cells[4], "Subtotal", true);

        foreach (var item in items)
        {
            var row = table.AddRow();
            WriteCell(row.Cells[0], item.Description);
            WriteCell(row.Cells[1], PdfReceiptFormatting.FormatQuantity(item.Quantity));
            WriteCell(row.Cells[2], PdfReceiptFormatting.FormatAmount(item.UnitPrice));
            WriteCell(row.Cells[3], PdfReceiptFormatting.FormatAmount(item.DiscountAmount));
            WriteCell(row.Cells[4], PdfReceiptFormatting.FormatAmount(item.Subtotal));
        }

        AddSpacer(section, 3);
    }

    private static void ComposeTotals(Section section, AuthorizedInvoice invoice)
    {
        var useTransparency = PdfReceiptFormatting.ShouldUseConsumerFiscalTransparency(invoice);

        AddSectionTitle(section, "Importes fiscales");

        var table = section.AddTable();
        table.Borders.Width = 0.5;
        table.AddColumn(Unit.FromCentimeter(9));
        table.AddColumn(Unit.FromCentimeter(8.5));

        if (!useTransparency)
        {
            AddKeyValueRow(table, "Importe gravado", PdfReceiptFormatting.FormatAmount(invoice.Totals.TaxableAmount));
            AddKeyValueRow(table, "Importe no gravado", PdfReceiptFormatting.FormatAmount(invoice.Totals.NonTaxedAmount));
            AddKeyValueRow(table, "Importe exento", PdfReceiptFormatting.FormatAmount(invoice.Totals.ExemptAmount));
            AddKeyValueRow(table, "IVA", PdfReceiptFormatting.FormatAmount(invoice.Totals.VatAmount));
            AddKeyValueRow(table, "Otros tributos", PdfReceiptFormatting.FormatAmount(invoice.Totals.OtherTaxesAmount));
        }
        if (PdfReceiptFormatting.ShouldDisplayCurrency(invoice.Currency))
        {
            AddKeyValueRow(table, "Moneda", PdfReceiptFormatting.FormatCurrencyDisplay(invoice.Currency));
        }
        AddKeyValueRow(table, "Importe total", PdfReceiptFormatting.FormatAmount(invoice.Totals.TotalAmount), emphasize: true);

        AddSpacer(section, 3);
    }

    private static void ComposeConsumerFiscalTransparency(Section section, AuthorizedInvoice invoice)
    {
        if (!PdfReceiptFormatting.ShouldUseConsumerFiscalTransparency(invoice))
        {
            return;
        }

        AddSectionTitle(section, "Transparencia fiscal al consumidor");
        section.AddParagraph("Régimen de Transparencia Fiscal al Consumidor Ley 27.743.");
        section.AddParagraph($"IVA contenido: {PdfReceiptFormatting.FormatAmount(invoice.Totals.VatAmount)}");
        section.AddParagraph($"Otros impuestos nacionales indirectos: {PdfReceiptFormatting.FormatAmount(invoice.Totals.OtherTaxesAmount)}");
        AddSpacer(section, 3);
    }

    private static void ComposeAssociatedVouchers(Section section, AuthorizedInvoice invoice)
    {
        if (invoice.AssociatedVouchers.Count == 0)
        {
            return;
        }

        AddSectionTitle(section, "Comprobantes asociados");

        foreach (var associatedVoucher in invoice.AssociatedVouchers)
        {
            var text = $"{associatedVoucher.VoucherType.Name} {associatedVoucher.PointOfSale:00000}-{associatedVoucher.VoucherNumber:00000000}";
            if (associatedVoucher.IssuedOn is { } issuedOn)
            {
                text += $" del {PdfReceiptFormatting.FormatDate(issuedOn)}";
            }

            if (associatedVoucher.IssuerCuit is { } issuerCuit)
            {
                text += $" (CUIT {issuerCuit})";
            }

            section.AddParagraph(text);
        }

        AddSpacer(section, 3);
    }

    private static void ComposeOperationalDetails(Section section, ReceiptRenderModel model)
    {
        if (string.IsNullOrWhiteSpace(model.PaymentDescription) && string.IsNullOrWhiteSpace(model.CashierName))
        {
            return;
        }

        AddSectionTitle(section, "Datos operativos");
        AddOptionalParagraph(section, string.IsNullOrWhiteSpace(model.PaymentDescription) ? null : $"Medio de pago: {model.PaymentDescription}");
        AddOptionalParagraph(section, string.IsNullOrWhiteSpace(model.CashierName) ? null : $"Cajero: {model.CashierName}");
        AddSpacer(section, 3);
    }

    private static void ComposeAuthorization(Section section, AuthorizedInvoice invoice)
    {
        AddSectionTitle(section, "Autorizacion fiscal");
        var paragraph = section.AddParagraph();
        paragraph.Format.Borders.Width = 0.75;
        paragraph.Format.Shading.Color = Colors.LightBlue;
        paragraph.Format.SpaceBefore = 3;
        paragraph.Format.SpaceAfter = 3;
        paragraph.Format.LeftIndent = 0;
        paragraph.Format.RightIndent = 0;
        paragraph.Format.FirstLineIndent = 0;
        paragraph.AddFormattedText($"{PdfReceiptFormatting.FormatAuthorizationCodeLabel(invoice.AuthorizationCodeType)}: ", TextFormat.Bold);
        paragraph.AddText(invoice.AuthorizationCode);
        paragraph.AddLineBreak();
        paragraph.AddFormattedText($"Vencimiento {PdfReceiptFormatting.FormatAuthorizationCodeLabel(invoice.AuthorizationCodeType)}: ", TextFormat.Bold);
        paragraph.AddText(PdfReceiptFormatting.FormatDate(invoice.AuthorizationDueDate));
        AddSpacer(section, 3);
    }

    private static void ComposeQrReference(Section section, QrRenderAssets qrAssets)
    {
        AddSectionTitle(section, "QR fiscal");
        var image = section.AddImage(qrAssets.ImagePath);
        image.LockAspectRatio = true;
        image.Width = Unit.FromCentimeter(3.5);

        var qrUrl = qrAssets.QrUrl;
        if (qrUrl is not null)
        {
            var paragraph = section.AddParagraph();
            paragraph.AddText("Validacion ARCA: ");
            paragraph.AddHyperlink(qrUrl.ToString(), HyperlinkType.Web).AddText(qrUrl.ToString());
        }

        AddSpacer(section, 3);
    }

    private static void ComposeFooterText(Section section, string? footerText)
    {
        if (string.IsNullOrWhiteSpace(footerText))
        {
            return;
        }

        var paragraph = section.AddParagraph(footerText);
        paragraph.Format.Alignment = ParagraphAlignment.Center;
        paragraph.Format.Font.Italic = true;
        paragraph.Format.SpaceBefore = 4;
    }

    private static void ComposeThermal(Section section, ReceiptRenderModel model, ReceiptPdfPageLayout layout, QrRenderAssets qrAssets)
    {
        var invoice = model.Invoice;

        var title = section.AddParagraph(model.Issuer.DisplayName);
        title.Style = "ThermalTitle";
        AddCentered(section, $"CUIT {model.Issuer.TaxId}");
        AddCentered(section, model.Issuer.VatConditionLabel);
        AddCentered(section, model.Issuer.Address);
        AddCentered(section, invoice.Series.VoucherType.Name, bold: true);
        AddCentered(section, PdfReceiptFormatting.FormatVoucherNumber(invoice));
        AddCentered(section, $"Fecha: {PdfReceiptFormatting.FormatDate(invoice.IssueDate)}");
        AddLine(section);

        ComposeThermalKeyValue(section, "Nombre", invoice.Customer.Name, layout);
        ComposeThermalKeyValue(section, "Documento", PdfReceiptFormatting.FormatCustomerDocument(invoice.Customer), layout);
        ComposeThermalKeyValue(section, "Cond. IVA", invoice.ReceiverVatCondition.Name, layout);
        ComposeThermalKeyValue(section, "Concepto", PdfReceiptFormatting.FormatConcept(invoice.Concept), layout);

        if (!string.IsNullOrWhiteSpace(model.PaymentDescription))
        {
            ComposeThermalKeyValue(section, "Pago", model.PaymentDescription, layout);
        }

        if (!string.IsNullOrWhiteSpace(model.CashierName))
        {
            ComposeThermalKeyValue(section, "Caja", model.CashierName, layout);
        }

        if (model.Items.Count > 0)
        {
            AddLine(section);
            AddCentered(section, "DETALLE", bold: true);

            foreach (var item in model.Items)
            {
                var description = section.AddParagraph(item.Description);
                description.Format.Font.Bold = true;
                section.AddParagraph($"{PdfReceiptFormatting.FormatQuantity(item.Quantity)} x {PdfReceiptFormatting.FormatAmount(item.UnitPrice)}");

                if (item.DiscountAmount > 0)
                {
                    section.AddParagraph($"Descuento: {PdfReceiptFormatting.FormatAmount(item.DiscountAmount)}");
                }

                section.AddParagraph($"Subtotal: {PdfReceiptFormatting.FormatAmount(item.Subtotal)}");
            }
        }

        AddLine(section);
        AddCentered(section, "TOTALES", bold: true);
        if (!PdfReceiptFormatting.ShouldUseConsumerFiscalTransparency(invoice))
        {
            ComposeThermalKeyValue(section, "Gravado", PdfReceiptFormatting.FormatAmount(invoice.Totals.TaxableAmount), layout);
            ComposeThermalKeyValue(section, "No gravado", PdfReceiptFormatting.FormatAmount(invoice.Totals.NonTaxedAmount), layout);
            ComposeThermalKeyValue(section, "Exento", PdfReceiptFormatting.FormatAmount(invoice.Totals.ExemptAmount), layout);
            ComposeThermalKeyValue(section, "IVA", PdfReceiptFormatting.FormatAmount(invoice.Totals.VatAmount), layout);
            ComposeThermalKeyValue(section, "Tributos", PdfReceiptFormatting.FormatAmount(invoice.Totals.OtherTaxesAmount), layout);
        }
        if (PdfReceiptFormatting.ShouldDisplayCurrency(invoice.Currency))
        {
            ComposeThermalKeyValue(section, "Moneda", PdfReceiptFormatting.FormatCurrencyDisplay(invoice.Currency), layout);
        }
        ComposeThermalKeyValue(section, "TOTAL", PdfReceiptFormatting.FormatAmount(invoice.Totals.TotalAmount), layout, true);

        if (PdfReceiptFormatting.ShouldUseConsumerFiscalTransparency(invoice))
        {
            AddLine(section);
            AddCentered(section, "TRANSPARENCIA FISCAL", bold: true);
            AddCentered(section, "Régimen de Transparencia Fiscal al Consumidor Ley 27.743.");
            ComposeThermalKeyValue(section, "IVA contenido", PdfReceiptFormatting.FormatAmount(invoice.Totals.VatAmount), layout);
            ComposeThermalKeyValue(section, "Imp. nac. indirectos", PdfReceiptFormatting.FormatAmount(invoice.Totals.OtherTaxesAmount), layout);
        }

        if (invoice.AssociatedVouchers.Count > 0)
        {
            AddLine(section);
            AddCentered(section, "ASOCIADOS", bold: true);
            foreach (var associatedVoucher in invoice.AssociatedVouchers)
            {
                section.AddParagraph($"{associatedVoucher.VoucherType.Name} {associatedVoucher.PointOfSale:00000}-{associatedVoucher.VoucherNumber:00000000}");
            }
        }

        AddLine(section);
        AddCentered(section, "AUTORIZACION", bold: true);
        ComposeThermalKeyValue(section, PdfReceiptFormatting.FormatAuthorizationCodeLabel(invoice.AuthorizationCodeType), invoice.AuthorizationCode, layout);
        ComposeThermalKeyValue(section, "Vencimiento", PdfReceiptFormatting.FormatDate(invoice.AuthorizationDueDate), layout);

        AddLine(section);
        AddCentered(section, "QR FISCAL", bold: true);
        var qrParagraph = section.AddParagraph();
        qrParagraph.Format.Alignment = ParagraphAlignment.Center;
        var qrImage = qrParagraph.AddImage(qrAssets.ImagePath);
        qrImage.LockAspectRatio = true;
        qrImage.Width = Unit.FromMillimeter(layout == ReceiptPdfPageLayout.Thermal58Mm ? 22 : 28);
        AddCentered(section, "Escanee el QR para validar.", bold: false);

        if (!string.IsNullOrWhiteSpace(model.FooterText))
        {
            AddLine(section);
            AddCentered(section, model.FooterText);
        }

        _ = layout;
    }

    private static void AddSectionTitle(Section section, string title)
    {
        var paragraph = section.AddParagraph(title);
        paragraph.Style = "ReceiptSectionTitle";
    }

    private static void AddSpacer(Section section, double millimeters)
    {
        var paragraph = section.AddParagraph();
        paragraph.Format.SpaceAfter = Unit.FromMillimeter(millimeters);
    }

    private static void AddOptionalParagraph(Section section, string? text)
    {
        if (!string.IsNullOrWhiteSpace(text))
        {
            section.AddParagraph(text);
        }
    }

    private static void AddOptionalParagraph(Cell cell, string? text)
    {
        if (!string.IsNullOrWhiteSpace(text))
        {
            cell.AddParagraph(text);
        }
    }

    private static void FillCell(Cell cell, string title, IReadOnlyList<string> lines)
    {
        cell.VerticalAlignment = VerticalAlignment.Top;
        var titleParagraph = cell.AddParagraph(title);
        titleParagraph.Format.Font.Bold = true;

        foreach (var line in lines)
        {
            cell.AddParagraph(line);
        }
    }

    private static void WriteCell(Cell cell, string text, bool bold = false)
    {
        var paragraph = cell.AddParagraph(text);
        paragraph.Format.Font.Bold = bold;
    }

    private static void AddKeyValueRow(Table table, string key, string value, bool emphasize = false)
    {
        var row = table.AddRow();
        if (emphasize)
        {
            row.Shading.Color = Colors.LightBlue;
        }

        var keyParagraph = row.Cells[0].AddParagraph(key);
        var valueParagraph = row.Cells[1].AddParagraph(value);
        valueParagraph.Format.Alignment = ParagraphAlignment.Right;

        if (emphasize)
        {
            keyParagraph.Format.Font.Bold = true;
            valueParagraph.Format.Font.Bold = true;
        }
    }

    private static void AddCentered(Section section, string? text, bool bold = false)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var paragraph = section.AddParagraph(text);
        paragraph.Format.Alignment = ParagraphAlignment.Center;
        paragraph.Format.Font.Bold = bold;
    }

    private static void AddLine(Section section)
    {
        var paragraph = section.AddParagraph(new string('-', 32));
        paragraph.Format.Alignment = ParagraphAlignment.Center;
        paragraph.Format.SpaceBefore = 2;
        paragraph.Format.SpaceAfter = 2;
    }

    private static void ComposeThermalKeyValue(Section section, string key, string value, ReceiptPdfPageLayout layout, bool bold = false)
    {
        var table = section.AddTable();
        table.Borders.Visible = false;
        if (layout == ReceiptPdfPageLayout.Thermal58Mm)
        {
            table.AddColumn(Unit.FromMillimeter(22));
            table.AddColumn(Unit.FromMillimeter(26));
        }
        else
        {
            table.AddColumn(Unit.FromMillimeter(30));
            table.AddColumn(Unit.FromMillimeter(38));
        }

        var row = table.AddRow();
        var left = row.Cells[0].AddParagraph(key);
        var right = row.Cells[1].AddParagraph(value);
        right.Format.Alignment = ParagraphAlignment.Right;

        if (bold)
        {
            left.Format.Font.Bold = true;
            right.Format.Font.Bold = true;
        }
    }

    private static QrRenderAssets CreateQrAssets(AuthorizedInvoice invoice, ArcaQrGenerator qrGenerator)
    {
        var qrUrl = invoice.QrUrl ?? qrGenerator.BuildUrl(invoice.QrPayload ?? qrGenerator.BuildPayload(invoice));
        var imageBytes = BuildQrBitmapBytes(qrUrl.ToString(), 8);
        var imagePath = Path.Combine(Path.GetTempPath(), $"arcanet-qr-{Guid.NewGuid():N}.bmp");
        File.WriteAllBytes(imagePath, imageBytes);
        return new QrRenderAssets(imagePath, qrUrl);
    }

    private static byte[] BuildQrBitmapBytes(string qrUrl, int pixelsPerModule)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(qrUrl, QRCodeGenerator.ECCLevel.Q);
        var bmpQr = new BitmapByteQRCode(data);
        return bmpQr.GetGraphic(pixelsPerModule);
    }

    private sealed class QrRenderAssets(string imagePath, Uri qrUrl) : IDisposable
    {
        public string ImagePath { get; } = imagePath;
        public Uri QrUrl { get; } = qrUrl;

        public void Dispose()
        {
            try
            {
                if (File.Exists(ImagePath))
                {
                    File.Delete(ImagePath);
                }
            }
            catch
            {
            }
        }
    }
}
