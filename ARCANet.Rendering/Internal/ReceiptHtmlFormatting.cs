using System.Globalization;
using System.Text.Encodings.Web;
using ARCANet.Invoices;

namespace ARCANet.Rendering.Internal;

internal static class ReceiptHtmlFormatting
{
    private static readonly CultureInfo SpanishArgentina = CultureInfo.GetCultureInfo("es-AR");

    public static HtmlEncoder HtmlEncoder { get; } = HtmlEncoder.Default;

    public static string FormatVoucherNumber(AuthorizedInvoice invoice) =>
        $"{invoice.Series.PointOfSale:00000}-{invoice.VoucherNumber:00000000}";

    public static string FormatConcept(InvoiceConcept concept) =>
        concept switch
        {
            InvoiceConcept.Products => "Productos",
            InvoiceConcept.Services => "Servicios",
            InvoiceConcept.ProductsAndServices => "Productos y servicios",
            _ => concept.ToString()
        };

    public static string FormatAuthorizationCodeLabel(AuthorizationCodeType codeType) =>
        codeType switch
        {
            AuthorizationCodeType.Cae => "CAE",
            AuthorizationCodeType.Caea => "CAEA",
            _ => "Codigo"
        };

    public static string FormatCustomerDocument(CustomerIdentity customer)
    {
        if (!string.IsNullOrWhiteSpace(customer.DocumentNumber))
        {
            return customer.DocumentTypeCode is { } documentTypeCode
                ? $"Tipo {documentTypeCode} {customer.DocumentNumber}"
                : customer.DocumentNumber;
        }

        return "No informado";
    }

    public static string FormatAmount(decimal amount) =>
        amount.ToString("N2", SpanishArgentina);

    public static string FormatExchangeRate(decimal exchangeRate) =>
        exchangeRate.ToString("N4", SpanishArgentina);

    public static string FormatCurrencyDisplay(CurrencyAmount currency)
    {
        ArgumentNullException.ThrowIfNull(currency);

        return $"{currency.Code} ({FormatExchangeRate(currency.ExchangeRate)})";
    }

    public static bool ShouldDisplayCurrency(CurrencyAmount currency)
    {
        ArgumentNullException.ThrowIfNull(currency);

        return !string.Equals(currency.Code, "PES", StringComparison.OrdinalIgnoreCase) ||
               currency.ExchangeRate != 1m;
    }

    public static bool ShouldUseConsumerFiscalTransparency(AuthorizedInvoice invoice)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        return invoice.Series.VoucherType.Name.EndsWith(" B", StringComparison.OrdinalIgnoreCase);
    }

    public static string FormatQuantity(decimal quantity) =>
        quantity.ToString("0.##", SpanishArgentina);

    public static string FormatDate(DateOnly date) =>
        date.ToString("dd/MM/yyyy", SpanishArgentina);
}
