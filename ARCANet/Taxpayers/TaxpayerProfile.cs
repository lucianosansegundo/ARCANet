using ARCANet.Invoices;

namespace ARCANet.Taxpayers;

public sealed record TaxpayerProfile
{
    public required long Cuit { get; init; }

    public string? DisplayName { get; init; }

    public string? PersonType { get; init; }

    public string? KeyStatus { get; init; }

    public IReadOnlyList<TaxpayerTax> GeneralTaxes { get; init; } = [];

    public TaxpayerMonotributoData? Monotributo { get; init; }

    public TaxpayerVatStatus VatStatus { get; init; }

    public ReceiverVatCondition? SuggestedReceiverVatCondition { get; init; }

    public IReadOnlyList<string> RegistryErrors { get; init; } = [];
}
