namespace Devv.CloudflareDdns;

using Microsoft.Extensions.Logging;

public class PublicIpProvider : IPublicIpProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PublicIpProvider> _logger;

    public PublicIpProvider(HttpClient httpClient, ILogger<PublicIpProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _logger.LogInformation("PublicIpProvider initialized");
    }
    public async Task<string> GetPublicIpAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching public IP from api.ipify.org");
        var response = await _httpClient.GetAsync("");
        
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception("Failed to get public IP");
        }

        return await response.Content.ReadAsStringAsync();
    }
}
