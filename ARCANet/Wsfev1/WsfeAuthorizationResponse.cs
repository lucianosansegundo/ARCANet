namespace ARCANet.Wsfev1;

internal sealed record WsfeAuthorizationResponse
{
    public required string HeaderResult { get; init; }
    public required string DetailResult { get; init; }
    public required string Reprocess { get; init; }
    public required DateTimeOffset ProcessedAtUtc { get; init; }
    public string? AuthorizationCode { get; init; }
    public DateOnly? AuthorizationDueDate { get; init; }
    public IReadOnlyList<WsfeResultIssue> Errors { get; init; } = [];
    public IReadOnlyList<WsfeResultIssue> Events { get; init; } = [];
    public IReadOnlyList<WsfeResultIssue> Observations { get; init; } = [];
}
