using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using ARCANet.Abstractions;
using ARCANet.Authentication;
using ARCANet.Configuration;
using ARCANet.Transport;
using ARCANet.Wsaa;

namespace ARCANet.Tests.Wsaa;

public sealed class WsaaAccessTicketProviderTests
{
    [Fact]
    public async Task GetAccessTicketAsync_FreshStoredTicket_AvoidsWsaaLogin()
    {
        var clock = new FakeClock();
        var certificate = CreateCertificate("SERIALNUMBER=CUIT 20123456789, CN=ARCANet Test");
        var store = new RecordingAccessTicketStore(
            new StoredAccessTicket("STORED_TOKEN", "STORED_SIGN", clock.UtcNow.AddMinutes(30), clock.UtcNow));
        var transport = new FakeSoapTransport();
        var provider = new WsaaAccessTicketProvider(
            new StubCertificateProvider(certificate),
            transport,
            clock,
            new WsaaOptions(),
            store);

        var ticket = await provider.GetAccessTicketAsync("wsfe");

        Assert.Equal("STORED_TOKEN", ticket.Token);
        Assert.Equal("STORED_SIGN", ticket.Sign);
        Assert.Empty(transport.Requests);
        Assert.NotNull(store.LastGetKey);
        Assert.Null(store.LastSavedTicket);
    }

    [Fact]
    public async Task GetAccessTicketAsync_ExpiredStoredTicket_TriggersWsaaLogin()
    {
        var clock = new FakeClock();
        var certificate = CreateCertificate("SERIALNUMBER=CUIT 20123456789, CN=ARCANet Test");
        var store = new RecordingAccessTicketStore(
            new StoredAccessTicket("EXPIRED_TOKEN", "EXPIRED_SIGN", clock.UtcNow.AddMinutes(4), clock.UtcNow.AddMinutes(-10)));
        var transport = new FakeSoapTransport(BuildWsaaResponse(
            token: "NEW_TOKEN",
            sign: "NEW_SIGN",
            expirationTime: "2026-05-15T00:00:00.000+00:00"));
        var provider = new WsaaAccessTicketProvider(
            new StubCertificateProvider(certificate),
            transport,
            clock,
            new WsaaOptions { RefreshBeforeExpiration = TimeSpan.FromMinutes(5) },
            store);

        var ticket = await provider.GetAccessTicketAsync("wsfe");

        Assert.Equal("NEW_TOKEN", ticket.Token);
        Assert.Single(transport.Requests);
    }

    [Fact]
    public async Task GetAccessTicketAsync_NewlyFetchedTicket_IsPersisted()
    {
        var clock = new FakeClock();
        var certificate = CreateCertificate("SERIALNUMBER=CUIT 20123456789, CN=ARCANet Test");
        var store = new RecordingAccessTicketStore();
        var transport = new FakeSoapTransport(BuildWsaaResponse(
            token: "TOKEN123",
            sign: "SIGN456",
            expirationTime: "2026-05-15T00:00:00.000+00:00"));
        var provider = new WsaaAccessTicketProvider(
            new StubCertificateProvider(certificate),
            transport,
            clock,
            new WsaaOptions { Environment = ArcaEnvironment.Homologation },
            store);

        var ticket = await provider.GetAccessTicketAsync("wsfe");

        Assert.Equal("TOKEN123", ticket.Token);
        Assert.NotNull(store.LastSavedKey);
        Assert.Equal(ArcaEnvironment.Homologation, store.LastSavedKey!.Environment);
        Assert.Equal("wsfe", store.LastSavedKey.Service);
        Assert.Equal(20123456789L, store.LastSavedKey.RepresentedCuit);
        Assert.Equal(certificate.Thumbprint, store.LastSavedKey.CertificateIdentifier);
        Assert.NotNull(store.LastSavedTicket);
        Assert.Equal(ticket.ExpiresAtUtc, store.LastSavedTicket!.ExpiresAtUtc);
        Assert.Equal(clock.UtcNow, store.LastSavedTicket.StoredAtUtc);
    }

    [Fact]
    public async Task GetAccessTicketAsync_DefaultConstructorPath_UsesSafeInMemoryStore()
    {
        var clock = new FakeClock();
        var certificate = CreateCertificate("SERIALNUMBER=CUIT 20123456789, CN=ARCANet Test");
        var transport = new FakeSoapTransport(BuildWsaaResponse(
            token: "TOKEN123",
            sign: "SIGN456",
            expirationTime: "2026-05-15T00:00:00.000+00:00"));
        var provider = new WsaaAccessTicketProvider(
            new StubCertificateProvider(certificate),
            transport,
            clock,
            new WsaaOptions());

        var first = await provider.GetAccessTicketAsync("wsfe");
        var second = await provider.GetAccessTicketAsync("wsfe");

        Assert.Equal(first, second);
        Assert.Single(transport.Requests);
    }

    [Fact]
    public async Task GetAccessTicketAsync_KeyDiffersByEnvironmentServiceCuitAndCertificateIdentifier()
    {
        var stored = new StoredAccessTicket("TOKEN", "SIGN", new DateTimeOffset(2026, 5, 16, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 5, 14, 12, 0, 0, TimeSpan.Zero));
        var clock = new FakeClock();

        var envStore = new RecordingAccessTicketStore(stored);
        await new WsaaAccessTicketProvider(
            new StubCertificateProvider(CreateCertificate("SERIALNUMBER=CUIT 20123456789, CN=Cert One")),
            new FakeSoapTransport(),
            clock,
            new WsaaOptions { Environment = ArcaEnvironment.Homologation },
            envStore).GetAccessTicketAsync("wsfe");

        var prodStore = new RecordingAccessTicketStore(stored);
        await new WsaaAccessTicketProvider(
            new StubCertificateProvider(CreateCertificate("SERIALNUMBER=CUIT 20123456789, CN=Cert One")),
            new FakeSoapTransport(),
            clock,
            new WsaaOptions { Environment = ArcaEnvironment.Production },
            prodStore).GetAccessTicketAsync("wsfe");

        var serviceStore = new RecordingAccessTicketStore(stored);
        await new WsaaAccessTicketProvider(
            new StubCertificateProvider(CreateCertificate("SERIALNUMBER=CUIT 20123456789, CN=Cert One")),
            new FakeSoapTransport(),
            clock,
            new WsaaOptions(),
            serviceStore).GetAccessTicketAsync("ws_sr_constancia_inscripcion");

        var cuitStore = new RecordingAccessTicketStore(stored);
        await new WsaaAccessTicketProvider(
            new StubCertificateProvider(CreateCertificate("SERIALNUMBER=CUIT 20987654321, CN=Cert One")),
            new FakeSoapTransport(),
            clock,
            new WsaaOptions(),
            cuitStore).GetAccessTicketAsync("wsfe");

        var certificateStore = new RecordingAccessTicketStore(stored);
        await new WsaaAccessTicketProvider(
            new StubCertificateProvider(CreateCertificate("SERIALNUMBER=CUIT 20123456789, CN=Cert Two")),
            new FakeSoapTransport(),
            clock,
            new WsaaOptions(),
            certificateStore).GetAccessTicketAsync("wsfe");

        Assert.NotEqual(envStore.LastGetKey, prodStore.LastGetKey);
        Assert.NotEqual(envStore.LastGetKey, serviceStore.LastGetKey);
        Assert.NotEqual(envStore.LastGetKey, cuitStore.LastGetKey);
        Assert.NotEqual(envStore.LastGetKey, certificateStore.LastGetKey);
    }

    [Fact]
    public async Task GetAccessTicketAsync_AlreadyAuthenticatedWithoutUsableStoredTicket_ThrowsClearTechnicalError()
    {
        var clock = new FakeClock();
        var certificate = CreateCertificate("SERIALNUMBER=CUIT 20123456789, CN=ARCANet Test");
        var transport = new FakeSoapTransport(new ArcaSoapTransportException(
            new Uri("https://wsaahomo.afip.gov.ar/ws/services/LoginCms"),
            "http://wsaa.view.sua.dvadac.desein.afip.gov/loginCms",
            HttpStatusCode.InternalServerError,
            "<soap:Fault><faultcode>coe.alreadyAuthenticated</faultcode><faultstring>El CEE ya posee un TA valido para el acceso al WSN solicitado</faultstring></soap:Fault>"));
        var provider = new WsaaAccessTicketProvider(
            new StubCertificateProvider(certificate),
            transport,
            clock,
            new WsaaOptions(),
            new RecordingAccessTicketStore());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => provider.GetAccessTicketAsync("wsfe"));

        Assert.Contains("alreadyAuthenticated", exception.Message, StringComparison.Ordinal);
        Assert.Contains("no usable locally stored access ticket", exception.Message, StringComparison.Ordinal);
        Assert.IsType<ArcaSoapTransportException>(exception.InnerException);
    }

    [Fact]
    public async Task GetAccessTicketAsync_FileStore_ReusesPersistedTicketAcrossProviderInstances()
    {
        using var fixture = new TemporaryDirectoryFixture();
        var clock = new FakeClock();
        var certificate = CreateCertificate("SERIALNUMBER=CUIT 20123456789, CN=ARCANet Test");
        var store = new FileAccessTicketStore(fixture.DirectoryPath);
        var firstTransport = new FakeSoapTransport(BuildWsaaResponse(
            token: "TOKEN123",
            sign: "SIGN456",
            expirationTime: "2026-05-15T00:00:00.000+00:00"));
        var secondTransport = new FakeSoapTransport();

        var firstProvider = new WsaaAccessTicketProvider(
            new StubCertificateProvider(certificate),
            firstTransport,
            clock,
            new WsaaOptions(),
            store);

        var firstTicket = await firstProvider.GetAccessTicketAsync("wsfe");

        var secondProvider = new WsaaAccessTicketProvider(
            new StubCertificateProvider(certificate),
            secondTransport,
            clock,
            new WsaaOptions(),
            new FileAccessTicketStore(fixture.DirectoryPath));

        var secondTicket = await secondProvider.GetAccessTicketAsync("wsfe");

        Assert.Equal(firstTicket, secondTicket);
        Assert.Single(firstTransport.Requests);
        Assert.Empty(secondTransport.Requests);
    }

    [Fact]
    public async Task GetAccessTicketAsync_ConcurrentRequestsForSameKey_PerformSingleWsaaLogin()
    {
        var clock = new FakeClock();
        var certificate = CreateCertificate("SERIALNUMBER=CUIT 20123456789, CN=ARCANet Test");
        var transport = new BlockingSoapTransport(
            BuildWsaaResponse(
                token: "TOKEN123",
                sign: "SIGN456",
                expirationTime: "2026-05-15T00:00:00.000+00:00"),
            delayMilliseconds: 75);
        var provider = new WsaaAccessTicketProvider(
            new StubCertificateProvider(certificate),
            transport,
            clock,
            new WsaaOptions(),
            new InMemoryAccessTicketStore());

        var firstTask = provider.GetAccessTicketAsync("wsfe");
        var secondTask = provider.GetAccessTicketAsync("wsfe");
        var tickets = await Task.WhenAll(firstTask, secondTask);

        Assert.Equal(tickets[0], tickets[1]);
        Assert.Equal(1, transport.RequestCount);
    }

    [Fact]
    public async Task GetAccessTicketAsync_ConcurrentRequestsAcrossProviderInstances_PerformSingleWsaaLogin()
    {
        var clock = new FakeClock();
        var certificate = CreateCertificate("SERIALNUMBER=CUIT 20123456789, CN=ARCANet Test");
        var sharedStore = new InMemoryAccessTicketStore();
        var transport = new BlockingSoapTransport(
            BuildWsaaResponse(
                token: "TOKEN123",
                sign: "SIGN456",
                expirationTime: "2026-05-15T00:00:00.000+00:00"),
            delayMilliseconds: 75);

        var firstProvider = new WsaaAccessTicketProvider(
            new StubCertificateProvider(certificate),
            transport,
            clock,
            new WsaaOptions(),
            sharedStore);
        var secondProvider = new WsaaAccessTicketProvider(
            new StubCertificateProvider(certificate),
            transport,
            clock,
            new WsaaOptions(),
            sharedStore);

        var firstTask = firstProvider.GetAccessTicketAsync("wsfe");
        var secondTask = secondProvider.GetAccessTicketAsync("wsfe");
        var tickets = await Task.WhenAll(firstTask, secondTask);

        Assert.Equal(tickets[0], tickets[1]);
        Assert.Equal(1, transport.RequestCount);
    }

    private static X509Certificate2 CreateCertificate(string subjectName)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            new X500DistinguishedName(subjectName),
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
    }

    private static string BuildWsaaResponse(string token, string sign, string expirationTime) =>
        $$"""
        <soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/">
          <soapenv:Body>
            <loginCmsResponse xmlns="http://wsaa.view.sua.dvadac.desein.afip.gov">
              <loginCmsReturn><![CDATA[<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <loginTicketResponse version="1.0">
          <header>
            <source>CN=wsaahomo</source>
            <destination>CN=test</destination>
            <uniqueId>1778760000</uniqueId>
            <generationTime>2026-05-14T11:50:00.000+00:00</generationTime>
            <expirationTime>{{expirationTime}}</expirationTime>
          </header>
          <credentials>
            <token>{{token}}</token>
            <sign>{{sign}}</sign>
          </credentials>
        </loginTicketResponse>]]></loginCmsReturn>
            </loginCmsResponse>
          </soapenv:Body>
        </soapenv:Envelope>
        """;

    private sealed class FakeClock : IClock
    {
        public DateTimeOffset UtcNow => new(2026, 5, 14, 12, 0, 0, TimeSpan.Zero);
    }

    private sealed class StubCertificateProvider(X509Certificate2 certificate) : ICertificateProvider
    {
        private readonly X509Certificate2 _certificate = certificate;

        public Task<X509Certificate2> GetCertificateAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_certificate);
    }

    private sealed class FakeSoapTransport(params object[] responses) : IArcaSoapTransport
    {
        private readonly Queue<object> _responses = new(responses);

        public List<ArcaSoapRequest> Requests { get; } = [];

        public Task<string> SendAsync(ArcaSoapRequest request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request);

            if (_responses.Count == 0)
            {
                throw new InvalidOperationException("No fake SOAP response configured.");
            }

            var next = _responses.Dequeue();
            return next switch
            {
                string response => Task.FromResult(response),
                Exception exception => Task.FromException<string>(exception),
                _ => throw new InvalidOperationException("Unsupported fake SOAP response type.")
            };
        }
    }

    private sealed class BlockingSoapTransport(string response, int delayMilliseconds) : IArcaSoapTransport
    {
        private readonly string _response = response;
        private readonly int _delayMilliseconds = delayMilliseconds;
        private int _requestCount;

        public int RequestCount => Volatile.Read(ref _requestCount);

        public async Task<string> SendAsync(ArcaSoapRequest request, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _requestCount);
            await Task.Delay(_delayMilliseconds, cancellationToken).ConfigureAwait(false);
            return _response;
        }
    }

    private sealed class RecordingAccessTicketStore(StoredAccessTicket? storedTicket = null) : IAccessTicketStore
    {
        private StoredAccessTicket? _storedTicket = storedTicket;

        public AccessTicketStoreKey? LastGetKey { get; private set; }

        public AccessTicketStoreKey? LastSavedKey { get; private set; }

        public StoredAccessTicket? LastSavedTicket { get; private set; }

        public Task<StoredAccessTicket?> GetAsync(AccessTicketStoreKey key, CancellationToken cancellationToken = default)
        {
            LastGetKey = key;
            return Task.FromResult(_storedTicket);
        }

        public Task SaveAsync(AccessTicketStoreKey key, StoredAccessTicket ticket, CancellationToken cancellationToken = default)
        {
            LastSavedKey = key;
            LastSavedTicket = ticket;
            _storedTicket = ticket;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(AccessTicketStoreKey key, CancellationToken cancellationToken = default)
        {
            _storedTicket = null;
            return Task.CompletedTask;
        }
    }

    private sealed class TemporaryDirectoryFixture : IDisposable
    {
        public TemporaryDirectoryFixture()
        {
            DirectoryPath = Path.Combine(Path.GetTempPath(), "ARCANet.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(DirectoryPath);
        }

        public string DirectoryPath { get; }

        public void Dispose()
        {
            if (Directory.Exists(DirectoryPath))
            {
                Directory.Delete(DirectoryPath, recursive: true);
            }
        }
    }
}
