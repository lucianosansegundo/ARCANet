namespace ARCANet.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
