namespace Devv.CloudflareDdns;

public interface IPublicIpProvider
{
    Task<string> GetPublicIpAsync(CancellationToken cancellationToken);
}