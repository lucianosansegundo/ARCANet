using System.Security.Cryptography.X509Certificates;

namespace ARCANet.Abstractions;

public interface ICertificateProvider
{
    Task<X509Certificate2> GetCertificateAsync(CancellationToken cancellationToken = default);
}
