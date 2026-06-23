namespace Devv.CloudflareDdns;

public interface IAcmeCertificateIssuer
{
    Task<IssuedAcmeCertificate> IssueCertificateAsync(
        AcmeCertificate certificate,
        AcmeCertificateOptions options,
        CancellationToken cancellationToken);
}

public sealed record IssuedAcmeCertificate(string CertificatePem, string PrivateKeyPem);
