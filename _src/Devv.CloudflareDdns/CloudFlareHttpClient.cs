using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Devv.CloudflareDdns;

public class CloudFlareHttpClient : ICloudFlareService, IPublicIpProvider
{
    private readonly ILogger<CloudFlareHttpClient> _logger;
    private readonly HttpClient _httpClient;
    private readonly CloudFlareOptions _options;

    public CloudFlareHttpClient(ILogger<CloudFlareHttpClient> logger, HttpClient httpClient,
        IOptions<CloudFlareOptions> options)
    {
        _logger = logger;
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task RunAsync(string publicIp)
    {
        foreach (var record in _options.Records)
        {
            try
            {
                _logger.LogInformation("Updating DNS record {recordName}", record.Name);
                await SendPublicIpToCloudFlareAsync(publicIp, record.ZoneId, record.DnsRecordId, record.Name);
                _logger.LogInformation("DNS record {recordName} updated", record.Name);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occurred while updating the DNS record");
            }
        }
    }

    private async Task SendPublicIpToCloudFlareAsync(string publicIp, string zoneId, string dnsRecordId,
        string recordName)
    {
        var body = new DnsRecord(dnsRecordId, recordName, publicIp, "Dynamic DNS Update");
        var request = new HttpRequestMessage(HttpMethod.Put,
            $"/client/v4/zones/{zoneId}/dns_records/{dnsRecordId}");
        request.Content = JsonContent.Create(body);
        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to send public IP to CloudFlare with response {response}",
                await response.Content.ReadAsStringAsync());
            throw new Exception("Failed to send public IP to CloudFlare");
        }
    }

    public async Task<string> GetPublicIpAsync(CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync("https://api.ipify.org");

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception("Failed to get public IP");
        }

        return await response.Content.ReadAsStringAsync();
    }

    public async Task UpdateDnsRecordsAsync(string publicIp, CancellationToken cancellationToken)
    {
        foreach (var record in _options.Records)
        {
            try
            {
                _logger.LogInformation("Updating DNS record {recordName}", record.Name);
                await SendPublicIpToCloudFlareAsync(publicIp, record.ZoneId, record.DnsRecordId, record.Name);
                _logger.LogInformation("DNS record {recordName} updated", record.Name);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occurred while updating the DNS record");
            }
        }
    }
}