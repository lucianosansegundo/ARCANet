using ARCANet.Configuration;

namespace ARCANet.Authentication;

public sealed record AccessTicketStoreKey(
    ArcaEnvironment Environment,
    string Service,
    long RepresentedCuit,
    string CertificateIdentifier);
