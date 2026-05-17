using ARCANet.Abstractions;
using ARCANet.Authentication;
using ARCANet.Transport;
using System.Collections.Concurrent;

namespace ARCANet.Wsaa;

public sealed class WsaaAccessTicketProvider : IAccessTicketProvider
{
    private static readonly ConcurrentDictionary<AccessTicketStoreKey, SemaphoreSlim> KeyLocks = new();

    private readonly ICertificateProvider _certificateProvider;
    private readonly WsaaClient _client;
    private readonly IClock _clock;
    private readonly WsaaOptions _options;
    private readonly IAccessTicketStore _store;

    public WsaaAccessTicketProvider(
        ICertificateProvider certificateProvider,
        IArcaSoapTransport transport,
        IClock clock,
        WsaaOptions? options = null)
        : this(certificateProvider, transport, clock, options, store: null)
    {
    }

    public WsaaAccessTicketProvider(
        ICertificateProvider certificateProvider,
        IArcaSoapTransport transport,
        IClock clock,
        WsaaOptions? options,
        IAccessTicketStore? store)
    {
        _certificateProvider = certificateProvider ?? throw new ArgumentNullException(nameof(certificateProvider));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _options = options ?? new WsaaOptions();
        _store = store ?? new InMemoryAccessTicketStore();
        _client = new WsaaClient(_certificateProvider, transport, clock, _options);
    }

    public async Task<AccessTicket> GetAccessTicketAsync(
        string service,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(service);

        var certificate = await _certificateProvider.GetCertificateAsync(cancellationToken).ConfigureAwait(false);
        var key = BuildStoreKey(service, certificate);
        var existing = await _store.GetAsync(key, cancellationToken).ConfigureAwait(false);
        if (IsUsable(existing))
        {
            return new AccessTicket(existing!.Token, existing.Sign, existing.ExpiresAtUtc);
        }

        if (_store is IAccessTicketStoreSynchronization synchronizedStore)
        {
            return await synchronizedStore.ExecuteSerializedAsync(
                key,
                token => GetOrRefreshAccessTicketAsync(service, certificate, key, token),
                cancellationToken).ConfigureAwait(false);
        }

        var gate = KeyLocks.GetOrAdd(key, static _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await GetOrRefreshAccessTicketAsync(service, certificate, key, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<AccessTicket> GetOrRefreshAccessTicketAsync(
        string service,
        System.Security.Cryptography.X509Certificates.X509Certificate2 certificate,
        AccessTicketStoreKey key,
        CancellationToken cancellationToken)
    {
        var existing = await _store.GetAsync(key, cancellationToken).ConfigureAwait(false);
        if (IsUsable(existing))
        {
            return new AccessTicket(existing!.Token, existing.Sign, existing.ExpiresAtUtc);
        }

        AccessTicket created;
        try
        {
            created = await _client.LoginAsync(service, certificate, cancellationToken).ConfigureAwait(false);
        }
        catch (ArcaSoapTransportException exception) when (IsAlreadyAuthenticatedFault(exception))
        {
            throw BuildAlreadyAuthenticatedException(service, key, exception);
        }

        await _store.SaveAsync(
            key,
            new StoredAccessTicket(created.Token, created.Sign, created.ExpiresAtUtc, _clock.UtcNow),
            cancellationToken).ConfigureAwait(false);

        return created;
    }

    private AccessTicketStoreKey BuildStoreKey(string service, System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) =>
        new(
            _options.Environment,
            service,
            CertificateIdentityResolver.GetRepresentedCuit(certificate),
            CertificateIdentityResolver.GetCertificateIdentifier(certificate));

    private bool IsUsable(StoredAccessTicket? ticket) =>
        ticket is not null &&
        ticket.ExpiresAtUtc > _clock.UtcNow.Add(_options.RefreshBeforeExpiration);

    private static bool IsAlreadyAuthenticatedFault(ArcaSoapTransportException exception)
    {
        var body = exception.ResponseBody;
        if (string.IsNullOrWhiteSpace(body))
        {
            return false;
        }

        return body.Contains("alreadyAuthenticated", StringComparison.OrdinalIgnoreCase) ||
               body.Contains("ya posee un TA valido", StringComparison.OrdinalIgnoreCase);
    }

    private static InvalidOperationException BuildAlreadyAuthenticatedException(
        string service,
        AccessTicketStoreKey key,
        ArcaSoapTransportException exception) =>
        new(
            $"WSAA returned alreadyAuthenticated for service '{service}' in environment '{key.Environment}', but no usable locally stored access ticket was available for represented CUIT '{key.RepresentedCuit}' and certificate '{key.CertificateIdentifier}'. A remote access ticket is still active and must be recovered from a shared store or allowed to expire before requesting a new one.",
            exception);
}
