using System.Text;
using ARCANet.Abstractions;
using ARCANet.Invoices;
using ARCANet.Rendering.Internal;
using ARCANet.Qr;

namespace ARCANet.Rendering;

public sealed class ThermalReceiptHtmlRenderer : IReceiptRenderer
{
    private readonly IArcaQrGenerator _qrGenerator;

    public ThermalReceiptHtmlRenderer()
        : this(new ArcaQrGenerator())
    {
    }

    public ThermalReceiptHtmlRenderer(IArcaQrGenerator qrGenerator)
    {
        _qrGenerator = qrGenerator ?? throw new ArgumentNullException(nameof(qrGenerator));
    }

    public string RenderHtml(ReceiptRenderModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var invoice = model.Invoice;
        var builder = new StringBuilder(capacity: 8192);

        builder.AppendLine("<!DOCTYPE html>");
        builder.AppendLine("<html lang=\"es-AR\">");
        builder.AppendLine("<head>");
        builder.AppendLine("  <meta charset=\"utf-8\">");
        builder.Append("  <title>");
        builder.Append(ReceiptHtmlFormatting.HtmlEncoder.Encode($"{invoice.Series.VoucherType.Name} {ReceiptHtmlFormatting.FormatVoucherNumber(invoice)}"));
        builder.AppendLine("</title>");
        builder.AppendLine("  <style>");
        builder.AppendLine(RenderCss());
        builder.AppendLine("  </style>");
        builder.AppendLine("</head>");
        builder.AppendLine("<body>");
        builder.AppendLine("  <article class=\"ticket\">");

        AppendHeader(builder, model);
        AppendReceiver(builder, model);
        AppendItems(builder, model);
        AppendTotals(builder, model);
        AppendConsumerFiscalTransparency(builder, model.Invoice);
        AppendAssociatedVouchers(builder, model);
        AppendAuthorization(builder, model);
        AppendQr(builder, model);
        AppendFooter(builder, model);

        builder.AppendLine("  </article>");
        builder.AppendLine("</body>");
        builder.AppendLine("</html>");

        return builder.ToString();
    }

    private static void AppendHeader(StringBuilder builder, ReceiptRenderModel model)
    {
        var invoice = model.Invoice;

        builder.AppendLine("    <header class=\"centered section\">");
        if (!string.IsNullOrWhiteSpace(model.LogoDataUrl))
        {
            builder.Append("      <img class=\"logo\" alt=\"Logo del emisor\" src=\"");
            builder.Append(ReceiptHtmlFormatting.HtmlEncoder.Encode(model.LogoDataUrl));
            builder.AppendLine("\">");
        }

        builder.Append("      <h1>");
        builder.Append(ReceiptHtmlFormatting.HtmlEncoder.Encode(model.Issuer.DisplayName));
        builder.AppendLine("</h1>");
        builder.Append("      <p>CUIT ");
        builder.Append(ReceiptHtmlFormatting.HtmlEncoder.Encode(model.Issuer.TaxId));
        builder.AppendLine("</p>");
        builder.Append("      <p>");
        builder.Append(ReceiptHtmlFormatting.HtmlEncoder.Encode(model.Issuer.VatConditionLabel));
        builder.AppendLine("</p>");

        if (!string.IsNullOrWhiteSpace(model.Issuer.Address))
        {
            builder.Append("      <p>");
            builder.Append(ReceiptHtmlFormatting.HtmlEncoder.Encode(model.Issuer.Address));
            builder.AppendLine("</p>");
        }

        builder.Append("      <h2>");
        builder.Append(ReceiptHtmlFormatting.HtmlEncoder.Encode(invoice.Series.VoucherType.Name));
        builder.AppendLine("</h2>");
        builder.Append("      <p>");
        builder.Append(ReceiptHtmlFormatting.HtmlEncoder.Encode(ReceiptHtmlFormatting.FormatVoucherNumber(invoice)));
        builder.AppendLine("</p>");
        builder.Append("      <p>Fecha: ");
        builder.Append(ReceiptHtmlFormatting.HtmlEncoder.Encode(ReceiptHtmlFormatting.FormatDate(invoice.IssueDate)));
        builder.AppendLine("</p>");
        builder.AppendLine("    </header>");
    }

    private static void AppendReceiver(StringBuilder builder, ReceiptRenderModel model)
    {
        var invoice = model.Invoice;

        builder.AppendLine("    <section class=\"section\">");
        builder.AppendLine("      <p class=\"section-title\">RECEPTOR</p>");
        AppendRow(builder, "Nombre", invoice.Customer.Name);
        AppendRow(builder, "Documento", ReceiptHtmlFormatting.FormatCustomerDocument(invoice.Customer));
        AppendRow(builder, "Condicion IVA", invoice.ReceiverVatCondition.Name);
        AppendRow(builder, "Concepto", ReceiptHtmlFormatting.FormatConcept(invoice.Concept));

        if (!string.IsNullOrWhiteSpace(model.PaymentDescription))
        {
            AppendRow(builder, "Pago", model.PaymentDescription);
        }

        if (!string.IsNullOrWhiteSpace(model.CashierName))
        {
            AppendRow(builder, "Caja", model.CashierName);
        }

        builder.AppendLine("    </section>");
    }

    private static void AppendItems(StringBuilder builder, ReceiptRenderModel model)
    {
        if (model.Items.Count == 0)
        {
            return;
        }

        builder.AppendLine("    <section class=\"section\">");
        builder.AppendLine("      <p class=\"section-title\">DETALLE</p>");

        foreach (var item in model.Items)
        {
            builder.AppendLine("      <div class=\"item\">");
            builder.Append("        <p class=\"item-description\">");
            builder.Append(ReceiptHtmlFormatting.HtmlEncoder.Encode(item.Description));
            builder.AppendLine("</p>");
            builder.Append("        <p class=\"item-values\">");
            builder.Append(ReceiptHtmlFormatting.HtmlEncoder.Encode($"{ReceiptHtmlFormatting.FormatQuantity(item.Quantity)} x {ReceiptHtmlFormatting.FormatAmount(item.UnitPrice)}"));

            if (item.DiscountAmount > 0)
            {
                builder.Append(" | Desc. ");
                builder.Append(ReceiptHtmlFormatting.HtmlEncoder.Encode(ReceiptHtmlFormatting.FormatAmount(item.DiscountAmount)));
            }

            builder.Append(" | Subtotal ");
            builder.Append(ReceiptHtmlFormatting.HtmlEncoder.Encode(ReceiptHtmlFormatting.FormatAmount(item.Subtotal)));
            builder.AppendLine("</p>");
            builder.AppendLine("      </div>");
        }

        builder.AppendLine("    </section>");
    }

    private static void AppendTotals(StringBuilder builder, ReceiptRenderModel model)
    {
        var invoice = model.Invoice;
        var useTransparency = ReceiptHtmlFormatting.ShouldUseConsumerFiscalTransparency(invoice);

        builder.AppendLine("    <section class=\"section\">");
        builder.AppendLine("      <p class=\"section-title\">TOTALES</p>");
        if (!useTransparency)
        {
            AppendRow(builder, "Importe gravado", ReceiptHtmlFormatting.FormatAmount(invoice.Totals.TaxableAmount));
            AppendRow(builder, "No gravado", ReceiptHtmlFormatting.FormatAmount(invoice.Totals.NonTaxedAmount));
            AppendRow(builder, "Exento", ReceiptHtmlFormatting.FormatAmount(invoice.Totals.ExemptAmount));
            AppendRow(builder, "IVA", ReceiptHtmlFormatting.FormatAmount(invoice.Totals.VatAmount));
            AppendRow(builder, "Tributos", ReceiptHtmlFormatting.FormatAmount(invoice.Totals.OtherTaxesAmount));
        }
        if (ReceiptHtmlFormatting.ShouldDisplayCurrency(invoice.Currency))
        {
            AppendRow(builder, "Moneda", ReceiptHtmlFormatting.FormatCurrencyDisplay(invoice.Currency));
        }
        AppendRow(builder, "TOTAL", ReceiptHtmlFormatting.FormatAmount(invoice.Totals.TotalAmount), "row total");
        builder.AppendLine("    </section>");
    }

    private static void AppendConsumerFiscalTransparency(StringBuilder builder, AuthorizedInvoice invoice)
    {
        if (!ReceiptHtmlFormatting.ShouldUseConsumerFiscalTransparency(invoice))
        {
            return;
        }

        builder.AppendLine("    <section class=\"section\">");
        builder.AppendLine("      <p class=\"section-title\">TRANSPARENCIA FISCAL</p>");
        builder.AppendLine("      <p>Régimen de Transparencia Fiscal al Consumidor Ley 27.743.</p>");
        AppendRow(builder, "IVA contenido", ReceiptHtmlFormatting.FormatAmount(invoice.Totals.VatAmount));
        AppendRow(builder, "Imp. nac. indirectos", ReceiptHtmlFormatting.FormatAmount(invoice.Totals.OtherTaxesAmount));
        builder.AppendLine("    </section>");
    }

    private static void AppendAssociatedVouchers(StringBuilder builder, ReceiptRenderModel model)
    {
        if (model.Invoice.AssociatedVouchers.Count == 0)
        {
            return;
        }

        builder.AppendLine("    <section class=\"section\">");
        builder.AppendLine("      <p class=\"section-title\">ASOCIADOS</p>");
        foreach (var associatedVoucher in model.Invoice.AssociatedVouchers)
        {
            var text = $"{associatedVoucher.VoucherType.Name} {associatedVoucher.PointOfSale:00000}-{associatedVoucher.VoucherNumber:00000000}";
            builder.Append("      <p>");
            builder.Append(ReceiptHtmlFormatting.HtmlEncoder.Encode(text));
            builder.AppendLine("</p>");
        }
        builder.AppendLine("    </section>");
    }

    private static void AppendAuthorization(StringBuilder builder, ReceiptRenderModel model)
    {
        var invoice = model.Invoice;

        builder.AppendLine("    <section class=\"section authorization\">");
        AppendRow(builder, ReceiptHtmlFormatting.FormatAuthorizationCodeLabel(invoice.AuthorizationCodeType), invoice.AuthorizationCode);
        AppendRow(builder, "Vencimiento", ReceiptHtmlFormatting.FormatDate(invoice.AuthorizationDueDate));
        builder.AppendLine("    </section>");
    }

    private void AppendQr(StringBuilder builder, ReceiptRenderModel model)
    {
        builder.AppendLine("    <section class=\"section centered qr-section\">");
        builder.AppendLine("      <p class=\"section-title\">QR FISCAL</p>");
        builder.AppendLine(_qrGenerator.BuildSvg(model.Invoice, pixelsPerModule: 5));
        builder.AppendLine("      <p class=\"qr-caption\">Escanee el QR para validar.</p>");

        builder.AppendLine("    </section>");
    }

    private static void AppendFooter(StringBuilder builder, ReceiptRenderModel model)
    {
        if (string.IsNullOrWhiteSpace(model.FooterText))
        {
            return;
        }

        builder.AppendLine("    <footer class=\"section centered\">");
        builder.Append("      <p>");
        builder.Append(ReceiptHtmlFormatting.HtmlEncoder.Encode(model.FooterText));
        builder.AppendLine("</p>");
        builder.AppendLine("    </footer>");
    }

    private static void AppendRow(StringBuilder builder, string label, string value, string cssClass = "row")
    {
        builder.Append("      <div class=\"");
        builder.Append(cssClass);
        builder.Append("\"><span>");
        builder.Append(ReceiptHtmlFormatting.HtmlEncoder.Encode(label));
        builder.Append("</span><strong>");
        builder.Append(ReceiptHtmlFormatting.HtmlEncoder.Encode(value));
        builder.AppendLine("</strong></div>");
    }

    private static string RenderCss() =>
        """
        :root {
          color-scheme: light;
          font-family: "Consolas", "Lucida Console", monospace;
        }

        * {
          box-sizing: border-box;
        }

        @page {
          size: 80mm auto;
          margin: 0;
        }

        body {
          margin: 0;
          background: #ffffff;
          color: #000000;
        }

        .ticket {
          width: 80mm;
          padding: 3mm;
        }

        .section {
          margin-top: 3mm;
          padding-top: 2mm;
          border-top: 1px dashed #000000;
        }

        .centered {
          text-align: center;
        }

        h1,
        h2,
        p {
          margin: 0 0 1.5mm;
        }

        h1 {
          font-size: 12pt;
        }

        h2 {
          font-size: 11pt;
          text-transform: uppercase;
        }

        p,
        span,
        strong {
          font-size: 9pt;
          line-height: 1.25;
        }

        .section-title {
          font-weight: 700;
          letter-spacing: 0.04em;
        }

        .row {
          display: flex;
          justify-content: space-between;
          gap: 6px;
          margin-bottom: 1.5mm;
        }

        .row span:first-child {
          padding-right: 6px;
        }

        .row.total {
          font-size: 10pt;
          border-top: 1px solid #000000;
          padding-top: 1.5mm;
        }

        .item {
          margin-bottom: 2mm;
        }

        .item-description {
          font-weight: 700;
        }

        .item-values,
        .qr-caption {
          overflow-wrap: anywhere;
        }

        .authorization {
          border-bottom: 1px dashed #000000;
          padding-bottom: 2mm;
        }

        .logo {
          max-width: 28mm;
          max-height: 14mm;
          margin-bottom: 2mm;
          object-fit: contain;
        }

        .qr-section svg {
          width: 28mm;
          height: 28mm;
        }
        """;
}
