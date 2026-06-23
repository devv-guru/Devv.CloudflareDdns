using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Devv.CloudflareDdns;

public class CloudFlareHttpClient : ICloudFlareService, IOriginCertificateApi, ICloudflareDnsChallengeService
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
        bool proxied,
        CancellationToken cancellationToken)
    {
        var body = new DnsRecord(
            dnsRecordId,
            recordName,
            publicIp,
            $"Dynamic DNS Update {DateTime.UtcNow:g}",
            proxied
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
                if (string.IsNullOrEmpty(record.ZoneId) || string.IsNullOrEmpty(record.DnsRecordId) || string.IsNullOrEmpty(record.Name))
                {
                    _logger.LogWarning("Skipping invalid record: ZoneId, DnsRecordId, and Name are required");
                    continue;
                }

                _logger.LogInformation("Updating DNS record {recordName}", record.Name);
                await SendPublicIpToCloudFlareAsync(publicIp, record.ZoneId, record.DnsRecordId, record.Name,
                    record.Proxied, cancellationToken);
                _logger.LogInformation("DNS record {recordName} updated", record.Name);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occurred while updating the DNS record");
            }
        }
    }

    public async Task<OriginCertificateResult> CreateOriginCertificateAsync(
        OriginCertificate certificate,
        string csr,
        CancellationToken cancellationToken)
    {
        var request = new OriginCertificateRequest(
            certificate.Hostnames,
            certificate.RequestedValidityDays,
            "origin-rsa",
            csr);

        var response = await _httpClient.PostAsJsonAsync(
            "/client/v4/certificates",
            request,
            CustomJsonContext.Default.OriginCertificateRequest,
            cancellationToken);

        var payload = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to create Cloudflare Origin CA certificate. Response: {Payload}", payload);
            throw new InvalidOperationException(
                $"Cloudflare Origin CA certificate creation failed with status {response.StatusCode}");
        }

        var apiResponse = await response.Content.ReadFromJsonAsync(
            CustomJsonContext.Default.CloudflareApiResponseOriginCertificateResult,
            cancellationToken);

        if (apiResponse?.Success != true || apiResponse.Result?.Certificate is null)
        {
            var error = apiResponse?.Errors.FirstOrDefault()?.Message ?? "Unknown Cloudflare API error";
            _logger.LogError(
                "Cloudflare Origin CA certificate creation failed. Error: {Error}. Response: {Payload}",
                error,
                payload);
            throw new InvalidOperationException($"Cloudflare Origin CA certificate creation failed: {error}");
        }

        return apiResponse.Result;
    }

    public async Task<string> CreateTxtRecordAsync(
        string zoneId,
        string name,
        string value,
        CancellationToken cancellationToken)
    {
        var body = new DnsRecord(
            string.Empty,
            name,
            value,
            $"ACME DNS-01 challenge {DateTime.UtcNow:g}",
            proxied: false,
            type: "TXT")
        {
            Ttl = 120
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"/client/v4/zones/{zoneId}/dns_records",
            body,
            CustomJsonContext.Default.DnsRecord,
            cancellationToken);

        var payload = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to create ACME DNS challenge record. Response: {Payload}", payload);
            throw new InvalidOperationException(
                $"Cloudflare DNS challenge record creation failed with status {response.StatusCode}");
        }

        var apiResponse = await response.Content.ReadFromJsonAsync(
            CustomJsonContext.Default.CloudflareApiResponseDnsChallengeRecordResult,
            cancellationToken);

        if (apiResponse?.Success != true || string.IsNullOrWhiteSpace(apiResponse.Result?.Id))
        {
            var error = apiResponse?.Errors.FirstOrDefault()?.Message ?? "Unknown Cloudflare API error";
            _logger.LogError(
                "Cloudflare DNS challenge record creation failed. Error: {Error}. Response: {Payload}",
                error,
                payload);
            throw new InvalidOperationException($"Cloudflare DNS challenge record creation failed: {error}");
        }

        return apiResponse.Result.Id;
    }

    public async Task DeleteTxtRecordAsync(
        string zoneId,
        string recordId,
        CancellationToken cancellationToken)
    {
        var response = await _httpClient.DeleteAsync(
            $"/client/v4/zones/{zoneId}/dns_records/{recordId}",
            cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogWarning(
            "Failed to delete ACME DNS challenge record {RecordId} in zone {ZoneId}. Response: {Payload}",
            recordId,
            zoneId,
            payload);
    }
}
