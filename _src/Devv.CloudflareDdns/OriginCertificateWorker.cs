using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Devv.CloudflareDdns;

public sealed class OriginCertificateWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<CloudFlareOptions> _options;
    private readonly ILogger<OriginCertificateWorker> _logger;

    public OriginCertificateWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<CloudFlareOptions> options,
        ILogger<OriginCertificateWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IOriginCertificateService>();
                await service.EnsureCertificatesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while managing Cloudflare Origin CA certificates");
            }

            await Task.Delay(GetCheckInterval(), stoppingToken);
        }
    }

    private TimeSpan GetCheckInterval()
    {
        var interval = _options.Value.OriginCertificates.CheckInterval;
        return interval > TimeSpan.Zero ? interval : TimeSpan.FromHours(12);
    }
}
