using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Devv.CloudflareDdns;

public class DynamicDnsWorker : BackgroundService
{
    private readonly ILogger<DynamicDnsWorker> _logger;
    private readonly IServiceProvider _serviceProvider;

    private string _publicIp = string.Empty;
    public DynamicDnsWorker(ILogger<DynamicDnsWorker> logger,
        IServiceProvider serviceProvider)
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
                using (var scope = _serviceProvider.CreateScope())
                {
                    var _ipProvider = scope.ServiceProvider.GetRequiredService<IPublicIpProvider>();
                    var _cloudFlareService = scope.ServiceProvider.GetRequiredService<ICloudFlareService>();
                    var publicIp = await _ipProvider.GetPublicIpAsync(stoppingToken);

                    if (_publicIp != publicIp)
                    {
                        _logger.LogInformation("Public IP has changed. Updating DNS record");
                        _publicIp = publicIp;
                        await _cloudFlareService.UpdateDnsRecordsAsync(_publicIp, stoppingToken);
                    }
                    else
                    {
                        _logger.LogInformation("Public IP has not changed");
                    }

                    await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occurred while updating the DNS record");
            }
        }
    }
}
