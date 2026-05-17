namespace ARCANet.Authentication;

public sealed record StoredAccessTicket(
    string Token,
    string Sign,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset StoredAtUtc);
