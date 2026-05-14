namespace ARCANet.Wsaa;

internal sealed record LoginTicketRequest(
    long UniqueId,
    DateTimeOffset GenerationTime,
    DateTimeOffset ExpirationTime,
    string Service);
