namespace Devv.CloudflareDdns;

public interface IOriginCertificateApi
{
    Task<OriginCertificateResult> CreateOriginCertificateAsync(
        OriginCertificate certificate,
        string csr,
        CancellationToken cancellationToken);
}
