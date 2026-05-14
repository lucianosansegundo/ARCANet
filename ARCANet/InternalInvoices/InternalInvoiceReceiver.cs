namespace ARCANet.InternalInvoices;

internal sealed record InternalInvoiceReceiver(
    string Name,
    bool IsConsumerFinal,
    int? DocumentTypeCode,
    string? DocumentNumber,
    int ReceiverVatConditionId,
    string ReceiverVatConditionName);
