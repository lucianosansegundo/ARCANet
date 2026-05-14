namespace ARCANet.Authentication;

public sealed record AccessTicket(
    string Token,
    string Sign,
    DateTimeOffset ExpiresAtUtc);
