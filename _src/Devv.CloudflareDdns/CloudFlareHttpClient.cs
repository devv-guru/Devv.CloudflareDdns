using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Devv.CloudflareDdns;

public class CloudFlareHttpClient : ICloudFlareService, IPublicIpProvider
{
    private readonly ILogger<CloudFlareHttpClient> _logger;
    private readonly HttpClient _httpClient;
    private readonly CloudFlareOptions _options;

    public CloudFlareHttpClient(ILogger<CloudFlareHttpClient> logger,
        HttpClient httpClient,
        IOptions<CloudFlareOptions> options)
    {
        _logger = logger;
        _httpClient = httpClient;
        _options = options.Value;
    }

    private async Task SendPublicIpToCloudFlareAsync(string publicIp,
        string zoneId,
        string dnsRecordId,
        string recordName,
        CancellationToken cancellationToken)
    {
        var body = new DnsRecord(
        dnsRecordId,
        recordName,
        publicIp,
        $"Dynamic DNS Update {DateTime.UtcNow:g}"
        );

        // Using PutAsJsonAsync with your source-generated context:
        var response = await _httpClient.PutAsJsonAsync(
        $"/client/v4/zones/{zoneId}/dns_records/{dnsRecordId}",
        body,
        CustomJsonContext.Default.DnsRecord,
        cancellationToken
        );

        if (!response.IsSuccessStatusCode)
        {
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
            "Failed to update DNS record {DnsRecordId} in zone {ZoneId}. Response: {Payload}",
            dnsRecordId, zoneId, payload
            );
            throw new InvalidOperationException(
            $"CloudFlare update failed with status {response.StatusCode}"
            );
        }

        _logger.LogInformation(
        "Successfully updated DNS record {DnsRecordId} to {Ip} in zone {ZoneId}",
        dnsRecordId, publicIp, zoneId
        );
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
        if (_options.Records == null || !_options.Records.Any())
        {
            _logger.LogWarning("No DNS records found to update");
            return;
        }

        _logger.LogInformation("Found {count} records to update", _options.Records.Count());
        foreach (var record in _options.Records)
        {
            try
            {
                _logger.LogInformation("Updating DNS record {recordName}", record.Name);
                await SendPublicIpToCloudFlareAsync(publicIp, record.ZoneId, record.DnsRecordId, record.Name, cancellationToken);
                _logger.LogInformation("DNS record {recordName} updated", record.Name);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occurred while updating the DNS record");
            }
        }
    }
}
