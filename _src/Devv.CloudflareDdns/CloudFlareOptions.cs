namespace Devv.CloudflareDdns;

public class CloudFlareOptions
{
    public const string SectionName = "CloudflareDdns";

    public string? Email { get; set; }
    public string? Key { get; set; }
    public Records[]? Records { get; set; }
}

public class Records
{
    public string? ZoneId { get; set; }
    public string? DnsRecordId { get; set; }
    public string? Name { get; set; }
}