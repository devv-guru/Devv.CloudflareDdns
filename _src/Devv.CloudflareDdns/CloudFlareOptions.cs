namespace Devv.CloudflareDdns;

public class CloudFlareOptions
{
    public const string SectionName = "CloudflareDdns";
    public Uri? ApiUrl { get; set; } = new Uri("https://api.cloudflare.com");
    public string? Email { get; set; }
    public string? Key { get; set; }
    public Record[]? Records { get; set; }
}

public class Record
{
    public string? ZoneId { get; set; }
    public string? DnsRecordId { get; set; }
    public string? Name { get; set; }
}