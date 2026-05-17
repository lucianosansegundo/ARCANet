using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

namespace ARCANet.Wsaa;

internal static partial class CertificateIdentityResolver
{
    public static long GetRepresentedCuit(X509Certificate2 certificate)
    {
        ArgumentNullException.ThrowIfNull(certificate);

        var match = CuitRegex().Match(certificate.Subject);
        if (!match.Success)
        {
            throw new InvalidOperationException(
                "The certificate subject does not contain a CUIT serialNumber in the expected 'CUIT 20123456789' format.");
        }

        return long.Parse(match.Groups["cuit"].Value, CultureInfo.InvariantCulture);
    }

    public static string GetCertificateIdentifier(X509Certificate2 certificate)
    {
        ArgumentNullException.ThrowIfNull(certificate);

        if (!string.IsNullOrWhiteSpace(certificate.Thumbprint))
        {
            return certificate.Thumbprint;
        }

        throw new InvalidOperationException("The certificate does not expose a thumbprint that can be used as an identifier.");
    }

    [GeneratedRegex(@"SERIALNUMBER\s*=\s*CUIT\s*(?<cuit>\d{11})", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CuitRegex();
}
