using ARCANet.Abstractions;
using ARCANet.Wsaa;

namespace ARCANet.Tests.Wsaa;

public sealed class LoginTicketRequestBuilderTests
{
    [Fact]
    public void BuildXml_ProducesExpectedWsaaLoginTicketRequest()
    {
        var builder = new LoginTicketRequestBuilder(
            new FakeClock(new DateTimeOffset(2026, 5, 14, 12, 0, 0, TimeSpan.Zero)),
            new WsaaOptions());

        var xml = builder.BuildXml("wsfe");

        Assert.Contains("<loginTicketRequest>", xml, StringComparison.Ordinal);
        Assert.Contains("<uniqueId>1778760000</uniqueId>", xml, StringComparison.Ordinal);
        Assert.Contains("<generationTime>2026-05-14T11:50:00+00:00</generationTime>", xml, StringComparison.Ordinal);
        Assert.Contains("<expirationTime>2026-05-14T12:10:00+00:00</expirationTime>", xml, StringComparison.Ordinal);
        Assert.Contains("<service>wsfe</service>", xml, StringComparison.Ordinal);
    }

    private sealed class FakeClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }
}
