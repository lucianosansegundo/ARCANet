using ARCANet.Abstractions;

namespace ARCANet;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
