using System.Globalization;
using System.Xml.Linq;
using ARCANet.Invoices;

namespace ARCANet.Taxpayers;

internal sealed class TaxpayerRegistryResponseParser
{
    public TaxpayerProfile? ParseGetPersonaResponse(string soap)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(soap);

        var document = XDocument.Parse(soap);
        var personaReturn = document.Descendants().FirstOrDefault(x => x.Name.LocalName == "personaReturn");
        if (personaReturn is null)
        {
            return null;
        }

        var general = GetChild(personaReturn, "datosGenerales");
        var registryError = GetChild(personaReturn, "errorConstancia");
        var profileNode = general ?? registryError;
        var generalTaxes = ParseGeneralTaxes(GetChild(personaReturn, "datosRegimenGeneral"));
        var monotributo = ParseMonotributo(GetChild(personaReturn, "datosMonotributo"));
        var vatStatus = InferVatStatus(generalTaxes, monotributo);

        return new TaxpayerProfile
        {
            Cuit = ParseLong(GetChildValue(profileNode, "idPersona")) ?? 0,
            DisplayName = BuildDisplayName(profileNode),
            PersonType = GetChildValue(general, "tipoPersona"),
            KeyStatus = GetChildValue(general, "estadoClave"),
            GeneralTaxes = generalTaxes,
            Monotributo = monotributo,
            VatStatus = vatStatus,
            SuggestedReceiverVatCondition = MapSuggestedReceiverVatCondition(vatStatus),
            RegistryErrors = ParseRegistryErrors(registryError)
        };
    }

    private static IReadOnlyList<string> ParseRegistryErrors(XElement? registryErrorNode) =>
        registryErrorNode?
            .Elements()
            .Where(x => x.Name.LocalName == "error")
            .Select(x => x.Value)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToArray() ?? [];

    private static IReadOnlyList<TaxpayerTax> ParseGeneralTaxes(XElement? regimeNode) =>
        regimeNode?
            .Elements()
            .Where(x => x.Name.LocalName == "impuesto")
            .Select(ParseTax)
            .ToArray() ?? [];

    private static TaxpayerMonotributoData? ParseMonotributo(XElement? monotributoNode)
    {
        if (monotributoNode is null)
        {
            return null;
        }

        var taxNode = GetChild(monotributoNode, "impuesto");
        if (taxNode is null)
        {
            return null;
        }

        var categoryNode = GetChild(monotributoNode, "categoriaMonotributo");

        return new TaxpayerMonotributoData(
            ParseTax(taxNode),
            GetChildValue(categoryNode, "descripcionCategoria"),
            ParseLong(GetChildValue(categoryNode, "idCategoria")));
    }

    private static TaxpayerTax ParseTax(XElement taxNode) =>
        new(
            ParseLong(GetChildValue(taxNode, "idImpuesto")) ?? 0,
            GetChildValue(taxNode, "descripcionImpuesto") ?? string.Empty,
            GetChildValue(taxNode, "estadoImpuesto"),
            ParseInt(GetChildValue(taxNode, "periodo")));

    private static TaxpayerVatStatus InferVatStatus(
        IReadOnlyList<TaxpayerTax> generalTaxes,
        TaxpayerMonotributoData? monotributo)
    {
        if (monotributo is not null)
        {
            return TaxpayerVatStatus.Monotributista;
        }

        if (generalTaxes.Any(x =>
                x.Description.Contains("IVA", StringComparison.OrdinalIgnoreCase) &&
                x.Description.Contains("EXENTO", StringComparison.OrdinalIgnoreCase)))
        {
            return TaxpayerVatStatus.Exempt;
        }

        if (generalTaxes.Any(x => x.Description.Contains("IVA", StringComparison.OrdinalIgnoreCase)))
        {
            return TaxpayerVatStatus.ResponsibleInscribed;
        }

        return TaxpayerVatStatus.Unknown;
    }

    private static ReceiverVatCondition? MapSuggestedReceiverVatCondition(TaxpayerVatStatus vatStatus) =>
        vatStatus switch
        {
            TaxpayerVatStatus.ResponsibleInscribed => new ReceiverVatCondition(1, "IVA Responsable Inscripto"),
            TaxpayerVatStatus.Exempt => new ReceiverVatCondition(4, "IVA Sujeto Exento"),
            TaxpayerVatStatus.Monotributista => new ReceiverVatCondition(6, "Responsable Monotributo"),
            _ => null
        };

    private static string? BuildDisplayName(XElement? general)
    {
        var businessName = GetChildValue(general, "razonSocial");
        if (!string.IsNullOrWhiteSpace(businessName))
        {
            return businessName;
        }

        var firstName = GetChildValue(general, "nombre");
        var lastName = GetChildValue(general, "apellido");
        return string.Join(" ", new[] { firstName, lastName }.Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    private static XElement? GetChild(XElement? parent, string localName) =>
        parent?.Elements().FirstOrDefault(x => x.Name.LocalName == localName);

    private static string? GetChildValue(XElement? parent, string localName) =>
        GetChild(parent, localName)?.Value;

    private static long? ParseLong(string? value) =>
        long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;

    private static int? ParseInt(string? value) =>
        int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
}
