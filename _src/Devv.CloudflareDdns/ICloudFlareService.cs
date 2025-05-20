namespace Devv.CloudflareDdns;

public interface ICloudFlareService
{
    Task UpdateDnsRecordsAsync(string publicIp, CancellationToken cancellationToken);
}