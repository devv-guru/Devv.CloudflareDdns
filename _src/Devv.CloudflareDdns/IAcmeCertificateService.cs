namespace Devv.CloudflareDdns;

public interface IAcmeCertificateService
{
    Task EnsureCertificatesAsync(CancellationToken cancellationToken);
}
