using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Devv.CloudflareDdns;

public class DynamicDnsWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DynamicDnsWorker> _logger;
    private string _publicIp = string.Empty;

    public DynamicDnsWorker(IServiceProvider serviceProvider, ILogger<DynamicDnsWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var cloudFlareHttpClient = scope.ServiceProvider.GetRequiredService<CloudFlareHttpClient>();
                var publicIp = await cloudFlareHttpClient.GetPublicIpAsync();

                if (_publicIp != publicIp)
                {
                    _logger.LogInformation("Public IP has changed. Updating DNS record");
                    _publicIp = publicIp;
                    await cloudFlareHttpClient.RunAsync(_publicIp);
                }
                else
                {
                    _logger.LogInformation("Public IP has not changed");
                }

                await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occurred while updating the DNS record");                
            }
        }
    }
}