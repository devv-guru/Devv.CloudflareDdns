namespace CloudflareDdns.Package;

public class CloudFlareOptions
{
    public const string SectionName = "CloudFlare";

    public required string? Email { get; set; }
    public required string? Key { get; set; }
    public Records[] Records { get; set; }
}

public class Records
{
    public string? ZoneId { get; set; }
    public string? DnsRecordId { get; set; }
    public string? Name { get; set; }
}