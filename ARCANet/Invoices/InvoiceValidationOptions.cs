namespace ARCANet.Invoices;

public sealed record InvoiceValidationOptions
{
    // Technical sanity windows only. These are configurable and not fiscal rules.
    public int MaxIssueDatePastDays { get; init; } = 3650;

    public int MaxIssueDateFutureDays { get; init; } = 30;

    public bool RequirePaymentDueDateForServiceConcepts { get; init; } = true;
}
