using System.Collections.Concurrent;
using ARCANet.Abstractions;
using ARCANet.Authentication;

namespace ARCANet.Wsaa;

public sealed class WsaaAccessTicketProvider : IAccessTicketProvider
{
    private readonly ConcurrentDictionary<string, AccessTicket> _cache = new(StringComparer.Ordinal);
    private readonly WsaaClient _client;
    private readonly IClock _clock;
    private readonly WsaaOptions _options;

    public WsaaAccessTicketProvider(
        ICertificateProvider certificateProvider,
        IArcaSoapTransport transport,
        IClock clock,
        WsaaOptions? options = null)
    {
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _options = options ?? new WsaaOptions();
        _client = new WsaaClient(certificateProvider, transport, clock, _options);
    }

    public async Task<AccessTicket> GetAccessTicketAsync(
        string service,
        CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(service, out var existing) &&
            existing.ExpiresAtUtc > _clock.UtcNow.Add(_options.RefreshBeforeExpiration))
        {
            return existing;
        }

        var created = await _client.LoginAsync(service, cancellationToken).ConfigureAwait(false);
        _cache[service] = created;
        return created;
    }
}
