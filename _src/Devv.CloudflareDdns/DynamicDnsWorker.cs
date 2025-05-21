using Devv.CloudflareDdns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class DynamicDnsWorker : BackgroundService
{
    private readonly ILogger<DynamicDnsWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private string _publicIp = string.Empty;

    public DynamicDnsWorker(
        ILogger<DynamicDnsWorker> logger,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var ipProvider = scope.ServiceProvider.GetRequiredService<IPublicIpProvider>();
                var cloudFlareService = scope.ServiceProvider.GetRequiredService<ICloudFlareService>();

                var publicIp = await ipProvider.GetPublicIpAsync(stoppingToken);
                if (_publicIp != publicIp)
                {
                    _logger.LogInformation("Public IP has changed. Updating DNS record");
                    _publicIp = publicIp;
                    await cloudFlareService.UpdateDnsRecordsAsync(publicIp, stoppingToken);
                }
                else
                {
                    _logger.LogInformation("Public IP has not changed");
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occurred while updating the DNS record");
            }

            // delay *outside* the scope so the scope is disposed immediately
            await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
        }
    }
}
