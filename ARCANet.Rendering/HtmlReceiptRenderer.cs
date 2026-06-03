using System.Text;
using ARCANet.Abstractions;
using ARCANet.Invoices;
using ARCANet.Rendering.Internal;
using ARCANet.Qr;

namespace ARCANet.Rendering;

public sealed class HtmlReceiptRenderer : IReceiptRenderer
{
    private readonly IArcaQrGenerator _qrGenerator;

    public HtmlReceiptRenderer()
        : this(new ArcaQrGenerator())
    {
    }

    public HtmlReceiptRenderer(IArcaQrGenerator qrGenerator)
    {
        _qrGenerator = qrGenerator ?? throw new ArgumentNullException(nameof(qrGenerator));
    }

    public string RenderHtml(ReceiptRenderModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(model.Invoice);
        ArgumentNullException.ThrowIfNull(model.Issuer);

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
        builder.AppendLine("  <article class=\"receipt\">");

        AppendHeader(builder, model);
        AppendParties(builder, invoice, model.Issuer);
        AppendConcept(builder, invoice);
        AppendItems(builder, model.Items);
        AppendTotals(builder, invoice);
        AppendConsumerFiscalTransparency(builder, invoice);
        AppendAssociatedVouchers(builder, invoice.AssociatedVouchers);
        AppendOperationalDetails(builder, model);
        AppendAuthorization(builder, invoice);
        AppendQr(builder, invoice);
        AppendFooter(builder, model.FooterText);

        builder.AppendLine("  </article>");
        builder.AppendLine("</body>");
        builder.AppendLine("</html>");

        return builder.ToString();
    }

    private static void AppendHeader(StringBuilder builder, ReceiptRenderModel model)
    {
        var invoice = model.Invoice;

        builder.AppendLine("    <header class=\"header\">");
        builder.AppendLine("      <div class=\"issuer-block\">");

        if (!string.IsNullOrWhiteSpace(model.LogoDataUrl))
        {
            builder.Append("        <img class=\"logo\" alt=\"Logo del emisor\" src=\"");
        builder.Append(ReceiptHtmlFormatting.HtmlEncoder.Encode(model.LogoDataUrl));
            builder.AppendLine("\">");
        }

        builder.Append("        <h1>");
        builder.Append(ReceiptHtmlFormatting.HtmlEncoder.Encode(model.Issuer.DisplayName));
        builder.AppendLine("</h1>");
        builder.Append("        <p class=\"issuer-meta\">CUIT ");
        builder.Append(ReceiptHtmlFormatting.HtmlEncoder.Encode(model.Issuer.TaxId));
        builder.AppendLine("</p>");
        builder.Append("        <p class=\"issuer-meta\">");
        builder.Append(ReceiptHtmlFormatting.HtmlEncoder.Encode(model.Issuer.VatConditionLabel));
        builder.AppendLine("</p>");

        if (!string.IsNullOrWhiteSpace(model.Issuer.Address))
        {
            builder.Append("        <p class=\"issuer-meta\">");
            builder.Append(ReceiptHtmlFormatting.HtmlEncoder.Encode(model.Issuer.Address));
            builder.AppendLine("</p>");
        }

        if (!string.IsNullOrWhiteSpace(model.Issuer.GrossIncomeNumber))
        {
            builder.Append("        <p class=\"issuer-meta\">Ingresos Brutos: ");
            builder.Append(ReceiptHtmlFormatting.HtmlEncoder.Encode(model.Issuer.GrossIncomeNumber));
            builder.AppendLine("</p>");
        }

        if (model.Issuer.BusinessStartDate is { } businessStartDate)
        {
            builder.Append("        <p class=\"issuer-meta\">Inicio de actividades: ");
            builder.Append(ReceiptHtmlFormatting.HtmlEncoder.Encode(ReceiptHtmlFormatting.FormatDate(businessStartDate)));
            builder.AppendLine("</p>");
        }

        builder.AppendLine("      </div>");
        builder.AppendLine("      <div class=\"voucher-block\">");
        builder.Append("        <p class=\"voucher-kind\">");
        builder.Append(ReceiptHtmlFormatting.HtmlEncoder.Encode(invoice.Series.VoucherType.Name));
        builder.AppendLine("</p>");
        builder.Append("        <p><strong>Punto de venta y numero:</strong> ");
        builder.Append(ReceiptHtmlFormatting.HtmlEncoder.Encode(ReceiptHtmlFormatting.FormatVoucherNumber(invoice)));
        builder.AppendLine("</p>");
        builder.Append("        <p><strong>Fecha de emision:</strong> ");
        builder.Append(ReceiptHtmlFormatting.HtmlEncoder.Encode(ReceiptHtmlFormatting.FormatDate(invoice.IssueDate)));
        builder.AppendLine("</p>");
        builder.Append("        <p><strong>Concepto:</strong> ");
        builder.Append(ReceiptHtmlFormatting.HtmlEncoder.Encode(ReceiptHtmlFormatting.FormatConcept(invoice.Concept)));
        builder.AppendLine("</p>");
        builder.AppendLine("      </div>");
        builder.AppendLine("    </header>");
    }

    private static void AppendParties(StringBuilder builder, AuthorizedInvoice invoice, IssuerDisplayInfo issuer)
    {
        builder.AppendLine("    <section class=\"section two-columns\">");
        builder.AppendLine("      <div>");
        builder.AppendLine("        <h2>Emisor</h2>");
        builder.Append("        <p><strong>Razon social:</strong> ");
        builder.Append(ReceiptHtmlFormatting.HtmlEncoder.Encode(issuer.DisplayName));
        builder.AppendLine("</p>");
        builder.Append("        <p><strong>CUIT:</strong> ");
        builder.Append(ReceiptHtmlFormatting.HtmlEncoder.Encode(issuer.TaxId));
        builder.AppendLine("</p>");
        builder.Append("        <p><strong>Condicion IVA:</strong> ");
        builder.Append(ReceiptHtmlFormatting.HtmlEncoder.Encode(issuer.VatConditionLabel));
        builder.AppendLine("</p>");
        builder.AppendLine("      </div>");
        builder.AppendLine("      <div>");
        builder.AppendLine("        <h2>Receptor</h2>");
        builder.Append("        <p><strong>Nombre:</strong> ");
        builder.Append(ReceiptHtmlFormatting.HtmlEncoder.Encode(invoice.Customer.Name));
        builder.AppendLine("</p>");
        builder.Append("        <p><strong>Documento:</strong> ");
        builder.Append(ReceiptHtmlFormatting.HtmlEncoder.Encode(ReceiptHtmlFormatting.FormatCustomerDocument(invoice.Customer)));
        builder.AppendLine("</p>");
        builder.Append("        <p><strong>Condicion IVA:</strong> ");
        builder.Append(ReceiptHtmlFormatting.HtmlEncoder.Encode(invoice.ReceiverVatCondition.Name));
        builder.AppendLine("</p>");
        builder.AppendLine("      </div>");
        builder.AppendLine("    </section>");
    }

    private static void AppendConcept(StringBuilder builder, AuthorizedInvoice invoice)
    {
        if (invoice.ServiceFrom is null && invoice.ServiceTo is null && invoice.PaymentDueDate is null)
        {
            return;
        }

        builder.AppendLine("    <section class=\"section\">");
        builder.AppendLine("      <h2>Periodo de servicio</h2>");

        if (invoice.ServiceFrom is { } serviceFrom)
        {
            builder.Append("      <p><strong>Desde:</strong> ");
            builder.Append(ReceiptHtmlFormatting.HtmlEncoder.Encode(ReceiptHtmlFormatting.FormatDate(serviceFrom)));
            builder.AppendLine("</p>");
        }

        if (invoice.ServiceTo is { } serviceTo)
        {
            builder.Append("      <p><strong>Hasta:</strong> ");
            builder.Append(ReceiptHtmlFormatting.HtmlEncoder.Encode(ReceiptHtmlFormatting.FormatDate(serviceTo)));
            builder.AppendLine("</p>");
        }

        if (invoice.PaymentDueDate is { } paymentDueDate)
        {
            builder.Append("      <p><strong>Vencimiento de pago:</strong> ");
            builder.Append(ReceiptHtmlFormatting.HtmlEncoder.Encode(ReceiptHtmlFormatting.FormatDate(paymentDueDate)));
            builder.AppendLine("</p>");
        }

        builder.AppendLine("    </section>");
    }

    private static void AppendItems(StringBuilder builder, IReadOnlyList<ReceiptLineItem> items)
    {
        if (items.Count == 0)
        {
            return;
        }

        builder.AppendLine("    <section class=\"section\">");
        builder.AppendLine("      <h2>Detalle comercial</h2>");
        builder.AppendLine("      <table>");
        builder.AppendLine("        <thead>");
        builder.AppendLine("          <tr><th>Descripcion</th><th>Cantidad</th><th>Precio unitario</th><th>Descuento</th><th>Subtotal</th></tr>");
        builder.AppendLine("        </thead>");
        builder.AppendLine("        <tbody>");

        foreach (var item in items)
        {
            builder.Append("          <tr><td>");
            builder.Append(ReceiptHtmlFormatting.HtmlEncoder.Encode(item.Description));
            builder.Append("</td><td>");
            builder.Append(ReceiptHtmlFormatting.HtmlEncoder.Encode(ReceiptHtmlFormatting.FormatQuantity(item.Quantity)));
            builder.Append("</td><td>");
            builder.Append(ReceiptHtmlFormatting.HtmlEncoder.Encode(ReceiptHtmlFormatting.FormatAmount(item.UnitPrice)));
            builder.Append("</td><td>");
            builder.Append(ReceiptHtmlFormatting.HtmlEncoder.Encode(ReceiptHtmlFormatting.FormatAmount(item.DiscountAmount)));
            builder.Append("</td><td>");
            builder.Append(ReceiptHtmlFormatting.HtmlEncoder.Encode(ReceiptHtmlFormatting.FormatAmount(item.Subtotal)));
            builder.AppendLine("</td></tr>");
        }

        builder.AppendLine("        </tbody>");
        builder.AppendLine("      </table>");
        builder.AppendLine("    </section>");
    }

    private static void AppendTotals(StringBuilder builder, AuthorizedInvoice invoice)
    {
        var useTransparency = ReceiptHtmlFormatting.ShouldUseConsumerFiscalTransparency(invoice);

        builder.AppendLine("    <section class=\"section\">");
        builder.AppendLine("      <h2>Importes fiscales</h2>");
        builder.AppendLine("      <div class=\"totals-grid\">");
        if (!useTransparency)
        {
            AppendKeyValue(builder, "Importe gravado", ReceiptHtmlFormatting.FormatAmount(invoice.Totals.TaxableAmount));
            AppendKeyValue(builder, "Importe no gravado", ReceiptHtmlFormatting.FormatAmount(invoice.Totals.NonTaxedAmount));
            AppendKeyValue(builder, "Importe exento", ReceiptHtmlFormatting.FormatAmount(invoice.Totals.ExemptAmount));
            AppendKeyValue(builder, "IVA", ReceiptHtmlFormatting.FormatAmount(invoice.Totals.VatAmount));
            AppendKeyValue(builder, "Otros tributos", ReceiptHtmlFormatting.FormatAmount(invoice.Totals.OtherTaxesAmount));
        }
        if (ReceiptHtmlFormatting.ShouldDisplayCurrency(invoice.Currency))
        {
            AppendKeyValue(builder, "Moneda", ReceiptHtmlFormatting.FormatCurrencyDisplay(invoice.Currency));
        }
        AppendKeyValue(builder, "Importe total", ReceiptHtmlFormatting.FormatAmount(invoice.Totals.TotalAmount), "key-value total");
        builder.AppendLine("      </div>");

        if (!useTransparency && invoice.VatItems.Count > 0)
        {
            builder.AppendLine("      <h3>Detalle de IVA</h3>");
            builder.AppendLine("      <table>");
            builder.AppendLine("        <thead>");
            builder.AppendLine("          <tr><th>Alicuota</th><th>Base imponible</th><th>Importe</th></tr>");
            builder.AppendLine("        </thead>");
            builder.AppendLine("        <tbody>");

            foreach (var vatItem in invoice.VatItems)
            {
                builder.Append("          <tr><td>");
                builder.Append(ReceiptHtmlFormatting.HtmlEncoder.Encode($"{ReceiptHtmlFormatting.FormatQuantity(vatItem.Rate)} %"));
                builder.Append("</td><td>");
                builder.Append(ReceiptHtmlFormatting.HtmlEncoder.Encode(ReceiptHtmlFormatting.FormatAmount(vatItem.BaseAmount)));
                builder.Append("</td><td>");
                builder.Append(ReceiptHtmlFormatting.HtmlEncoder.Encode(ReceiptHtmlFormatting.FormatAmount(vatItem.Amount)));
                builder.AppendLine("</td></tr>");
            }

            builder.AppendLine("        </tbody>");
            builder.AppendLine("      </table>");
        }

        if (!useTransparency && invoice.Tributes.Count > 0)
        {
            builder.AppendLine("      <h3>Otros tributos</h3>");
            builder.AppendLine("      <table>");
            builder.AppendLine("        <thead>");
            builder.AppendLine("          <tr><th>Descripcion</th><th>Alicuota</th><th>Base</th><th>Importe</th></tr>");
            builder.AppendLine("        </thead>");
            builder.AppendLine("        <tbody>");

            foreach (var tribute in invoice.Tributes)
            {
                builder.Append("          <tr><td>");
                builder.Append(ReceiptHtmlFormatting.HtmlEncoder.Encode(tribute.Description ?? $"Tributo {tribute.Id}"));
                builder.Append("</td><td>");
                builder.Append(ReceiptHtmlFormatting.HtmlEncoder.Encode($"{ReceiptHtmlFormatting.FormatQuantity(tribute.Rate)} %"));
                builder.Append("</td><td>");
                builder.Append(ReceiptHtmlFormatting.HtmlEncoder.Encode(ReceiptHtmlFormatting.FormatAmount(tribute.BaseAmount)));
                builder.Append("</td><td>");
                builder.Append(ReceiptHtmlFormatting.HtmlEncoder.Encode(ReceiptHtmlFormatting.FormatAmount(tribute.Amount)));
                builder.AppendLine("</td></tr>");
            }

            builder.AppendLine("        </tbody>");
            builder.AppendLine("      </table>");
        }

        builder.AppendLine("    </section>");
    }

    private static void AppendConsumerFiscalTransparency(StringBuilder builder, AuthorizedInvoice invoice)
    {
        if (!ReceiptHtmlFormatting.ShouldUseConsumerFiscalTransparency(invoice))
        {
            return;
        }

        builder.AppendLine("    <section class=\"section transparency\">");
        builder.AppendLine("      <h2>Transparencia fiscal al consumidor</h2>");
        builder.AppendLine("      <p>Régimen de Transparencia Fiscal al Consumidor Ley 27.743.</p>");
        builder.Append("      <p><strong>IVA contenido:</strong> ");
        builder.Append(ReceiptHtmlFormatting.HtmlEncoder.Encode(ReceiptHtmlFormatting.FormatAmount(invoice.Totals.VatAmount)));
        builder.AppendLine("</p>");
        builder.Append("      <p><strong>Otros impuestos nacionales indirectos:</strong> ");
        builder.Append(ReceiptHtmlFormatting.HtmlEncoder.Encode(ReceiptHtmlFormatting.FormatAmount(invoice.Totals.OtherTaxesAmount)));
        builder.AppendLine("</p>");
        builder.AppendLine("    </section>");
    }

    private static void AppendAssociatedVouchers(StringBuilder builder, IReadOnlyList<AssociatedVoucher> associatedVouchers)
    {
        if (associatedVouchers.Count == 0)
        {
            return;
        }

        builder.AppendLine("    <section class=\"section\">");
        builder.AppendLine("      <h2>Comprobantes asociados</h2>");
        builder.AppendLine("      <ul class=\"associated-vouchers\">");

        foreach (var associatedVoucher in associatedVouchers)
        {
            var text = $"{associatedVoucher.VoucherType.Name} {associatedVoucher.PointOfSale:00000}-{associatedVoucher.VoucherNumber:00000000}";
            if (associatedVoucher.IssuedOn is { } issuedOn)
            {
                text += $" del {ReceiptHtmlFormatting.FormatDate(issuedOn)}";
            }

            if (associatedVoucher.IssuerCuit is { } issuerCuit)
            {
                text += $" (CUIT {issuerCuit})";
            }

            builder.Append("        <li>");
            builder.Append(ReceiptHtmlFormatting.HtmlEncoder.Encode(text));
            builder.AppendLine("</li>");
        }

        builder.AppendLine("      </ul>");
        builder.AppendLine("    </section>");
    }

    private static void AppendOperationalDetails(StringBuilder builder, ReceiptRenderModel model)
    {
        if (string.IsNullOrWhiteSpace(model.PaymentDescription) &&
            string.IsNullOrWhiteSpace(model.CashierName))
        {
            return;
        }

        builder.AppendLine("    <section class=\"section\">");
        builder.AppendLine("      <h2>Datos operativos</h2>");

        if (!string.IsNullOrWhiteSpace(model.PaymentDescription))
        {
            builder.Append("      <p><strong>Medio de pago:</strong> ");
            builder.Append(ReceiptHtmlFormatting.HtmlEncoder.Encode(model.PaymentDescription));
            builder.AppendLine("</p>");
        }

        if (!string.IsNullOrWhiteSpace(model.CashierName))
        {
            builder.Append("      <p><strong>Cajero:</strong> ");
            builder.Append(ReceiptHtmlFormatting.HtmlEncoder.Encode(model.CashierName));
            builder.AppendLine("</p>");
        }

        builder.AppendLine("    </section>");
    }

    private static void AppendAuthorization(StringBuilder builder, AuthorizedInvoice invoice)
    {
        builder.AppendLine("    <section class=\"section authorization\">");
        builder.AppendLine("      <h2>Autorizacion fiscal</h2>");
        builder.Append("      <p><strong>");
        builder.Append(ReceiptHtmlFormatting.HtmlEncoder.Encode(ReceiptHtmlFormatting.FormatAuthorizationCodeLabel(invoice.AuthorizationCodeType)));
        builder.Append(":</strong> ");
        builder.Append(ReceiptHtmlFormatting.HtmlEncoder.Encode(invoice.AuthorizationCode));
        builder.AppendLine("</p>");
        builder.Append("      <p><strong>Vencimiento ");
        builder.Append(ReceiptHtmlFormatting.HtmlEncoder.Encode(ReceiptHtmlFormatting.FormatAuthorizationCodeLabel(invoice.AuthorizationCodeType)));
        builder.Append(":</strong> ");
        builder.Append(ReceiptHtmlFormatting.HtmlEncoder.Encode(ReceiptHtmlFormatting.FormatDate(invoice.AuthorizationDueDate)));
        builder.AppendLine("</p>");
        builder.AppendLine("    </section>");
    }

    private void AppendQr(StringBuilder builder, AuthorizedInvoice invoice)
    {
        builder.AppendLine("    <section class=\"section qr-section\">");
        builder.AppendLine("      <h2>QR fiscal</h2>");
        builder.AppendLine("      <div class=\"qr-wrapper\">");
        builder.AppendLine(_qrGenerator.BuildSvg(invoice, pixelsPerModule: 8));

        if (invoice.QrUrl is not null)
        {
            builder.Append("        <p class=\"qr-url\">");
            builder.Append(ReceiptHtmlFormatting.HtmlEncoder.Encode(invoice.QrUrl.ToString()));
            builder.AppendLine("</p>");
        }

        builder.AppendLine("      </div>");
        builder.AppendLine("    </section>");
    }

    private static void AppendFooter(StringBuilder builder, string? footerText)
    {
        if (string.IsNullOrWhiteSpace(footerText))
        {
            return;
        }

        builder.AppendLine("    <footer class=\"section footer\">");
        builder.Append("      <p>");
        builder.Append(ReceiptHtmlFormatting.HtmlEncoder.Encode(footerText));
        builder.AppendLine("</p>");
        builder.AppendLine("    </footer>");
    }

    private static void AppendKeyValue(StringBuilder builder, string key, string value, string cssClass = "key-value")
    {
        builder.Append("        <div class=\"");
        builder.Append(cssClass);
        builder.Append("\"><span>");
        builder.Append(ReceiptHtmlFormatting.HtmlEncoder.Encode(key));
        builder.Append("</span><strong>");
        builder.Append(ReceiptHtmlFormatting.HtmlEncoder.Encode(value));
        builder.AppendLine("</strong></div>");
    }

    private static string RenderCss() =>
        """
        :root {
          color-scheme: light;
          font-family: "Segoe UI", Arial, sans-serif;
        }

        * {
          box-sizing: border-box;
        }

        body {
          margin: 0;
          padding: 24px;
          background: #f3f4f6;
          color: #111827;
        }

        .receipt {
          max-width: 920px;
          margin: 0 auto;
          padding: 32px;
          background: #ffffff;
          border: 1px solid #d1d5db;
        }

        .header,
        .two-columns {
          display: grid;
          grid-template-columns: repeat(2, minmax(0, 1fr));
          gap: 24px;
        }

        .section {
          margin-top: 24px;
        }

        .issuer-block h1,
        .voucher-kind,
        h2,
        h3 {
          margin: 0 0 8px;
        }

        .voucher-kind {
          font-size: 1.5rem;
          font-weight: 700;
          text-transform: uppercase;
        }

        .issuer-meta,
        p,
        li {
          margin: 0 0 6px;
          line-height: 1.4;
        }

        .logo {
          max-width: 180px;
          max-height: 80px;
          margin-bottom: 12px;
          object-fit: contain;
        }

        table {
          width: 100%;
          border-collapse: collapse;
          margin-top: 12px;
        }

        th,
        td {
          border: 1px solid #d1d5db;
          padding: 10px;
          text-align: left;
          vertical-align: top;
        }

        th {
          background: #f9fafb;
        }

        .totals-grid {
          display: grid;
          grid-template-columns: repeat(2, minmax(0, 1fr));
          gap: 12px;
          margin-top: 12px;
        }

        .key-value {
          display: flex;
          justify-content: space-between;
          gap: 16px;
          padding: 10px 12px;
          border: 1px solid #d1d5db;
          background: #f9fafb;
        }

        .key-value.total {
          background: #e5eefb;
          border-color: #93c5fd;
          font-size: 1.05rem;
        }

        .authorization {
          border: 1px solid #bfdbfe;
          padding: 16px;
          background: #eff6ff;
        }

        .transparency {
          border: 1px solid #fcd34d;
          padding: 16px;
          background: #fffbeb;
        }

        .associated-vouchers {
          margin: 12px 0 0;
          padding-left: 20px;
        }

        .qr-section {
          page-break-inside: avoid;
        }

        .qr-wrapper {
          display: inline-flex;
          flex-direction: column;
          gap: 12px;
          align-items: flex-start;
          padding: 16px;
          border: 1px solid #d1d5db;
          background: #ffffff;
        }

        .qr-wrapper svg {
          width: 180px;
          height: 180px;
        }

        .qr-url {
          overflow-wrap: anywhere;
          font-size: 0.85rem;
          color: #4b5563;
        }

        @media print {
          body {
            padding: 0;
            background: #ffffff;
          }

          .receipt {
            max-width: none;
            border: none;
            padding: 0;
          }
        }

        @media (max-width: 720px) {
          body {
            padding: 12px;
          }

          .header,
          .two-columns,
          .totals-grid {
            grid-template-columns: 1fr;
          }

          .receipt {
            padding: 20px;
          }

          th,
          td {
            padding: 8px;
          }
        }
        """;
}
