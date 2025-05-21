namespace Devv.CloudflareDdns;

using System.ComponentModel.DataAnnotations;

public class CloudFlareOptions
{
    public const string SectionName = "CloudflareDdns";

    [Required, Url]
    public Uri? ApiUrl { get; set; } = new Uri("https://api.cloudflare.com");
    
    public string? Email { get; set; }

    [Required]
    public string? Key { get; set; }

    [Required]
    public Record[]? Records { get; set; }
}

public class Record
{
    [Required]
    public string? ZoneId { get; set; }

    [Required]
    public string? DnsRecordId { get; set; }

    [Required]
    public string? Name { get; set; }
}
