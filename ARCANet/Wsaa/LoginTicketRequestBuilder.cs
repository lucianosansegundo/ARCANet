using System.Globalization;
using System.Xml.Linq;
using ARCANet.Abstractions;

namespace ARCANet.Wsaa;

internal sealed class LoginTicketRequestBuilder(IClock clock, WsaaOptions options)
{
    private readonly IClock _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    private readonly WsaaOptions _options = options ?? throw new ArgumentNullException(nameof(options));

    public LoginTicketRequest Build(string service)
    {
        if (string.IsNullOrWhiteSpace(service))
        {
            throw new ArgumentException("Service is required.", nameof(service));
        }

        var generationTime = _clock.UtcNow.Add(_options.GenerationTimeOffset);
        var expirationTime = generationTime.Add(_options.RequestLifetime);

        return new LoginTicketRequest(
            UniqueId: _clock.UtcNow.ToUnixTimeSeconds(),
            GenerationTime: generationTime,
            ExpirationTime: expirationTime,
            Service: service);
    }

    public string BuildXml(string service)
    {
        var request = Build(service);

        var document = new XDocument(
            new XElement("loginTicketRequest",
                new XElement("header",
                    new XElement("uniqueId", request.UniqueId.ToString(CultureInfo.InvariantCulture)),
                    new XElement("generationTime", request.GenerationTime.ToString("yyyy-MM-dd'T'HH:mm:ssK", CultureInfo.InvariantCulture)),
                    new XElement("expirationTime", request.ExpirationTime.ToString("yyyy-MM-dd'T'HH:mm:ssK", CultureInfo.InvariantCulture))),
                new XElement("service", request.Service)));

        return document.ToString(SaveOptions.DisableFormatting);
    }
}
