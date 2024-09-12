namespace CloudflareDdns.Package;

public class CloudFlareOptions
{
    public const string SectionName = "CloudFlare";

    public required string? Email { get; set; }
    public required string? Key { get; set; }
    public Records[]? Records { get; set; }
}

public class Records
{
    public required string? ZoneId { get; set; }
    public required string? DnsRecordId { get; set; }
    public required string? Name { get; set; }
}