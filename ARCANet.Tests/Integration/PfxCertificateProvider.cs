using System.Security.Cryptography.X509Certificates;
using ARCANet.Abstractions;

namespace ARCANet.Tests.Integration;

internal sealed class PfxCertificateProvider(string certificatePath, string? certificatePassword) : ICertificateProvider
{
    private readonly string _certificatePath = certificatePath ?? throw new ArgumentNullException(nameof(certificatePath));
    private readonly string? _certificatePassword = certificatePassword;

    public Task<X509Certificate2> GetCertificateAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var certificate = X509CertificateLoader.LoadPkcs12FromFile(
            _certificatePath,
            _certificatePassword,
            X509KeyStorageFlags.EphemeralKeySet | X509KeyStorageFlags.Exportable);

        return Task.FromResult(certificate);
    }
}
