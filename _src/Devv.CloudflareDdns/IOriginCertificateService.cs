namespace Devv.CloudflareDdns;

public interface IOriginCertificateService
{
    Task EnsureCertificatesAsync(CancellationToken cancellationToken);
}
