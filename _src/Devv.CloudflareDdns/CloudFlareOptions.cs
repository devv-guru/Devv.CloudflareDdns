namespace Devv.CloudflareDdns;

using System.ComponentModel.DataAnnotations;

public class CloudFlareOptions
{
    public const string SectionName = "CloudflareDdns";

    public Uri? ApiUrl { get; set; } = new Uri("https://api.cloudflare.com");

    public string? Email { get; set; }

    [Required]
    public string? Key { get; set; }

    [Required]
    public Record[] Records { get; set; } = Array.Empty<Record>();

    public OriginCertificateOptions OriginCertificates { get; set; } = new();
}

public class Record
{
    [Required]
    public string? ZoneId { get; set; }

    [Required]
    public string? DnsRecordId { get; set; }

    [Required]
    public string? Name { get; set; }

    public bool Proxied { get; set; } = true;
}

public class OriginCertificateOptions
{
    public bool Enabled { get; set; }

    public TimeSpan CheckInterval { get; set; } = TimeSpan.FromHours(12);

    public TimeSpan RenewBeforeExpiry { get; set; } = TimeSpan.FromDays(30);

    public string? ReloadCommand { get; set; }

    public OriginCertificate[] Certificates { get; set; } = Array.Empty<OriginCertificate>();
}

public class OriginCertificate
{
    [Required]
    public string[] Hostnames { get; set; } = Array.Empty<string>();

    [Required]
    public string? CertificatePath { get; set; }

    [Required]
    public string? PrivateKeyPath { get; set; }

    public int RequestedValidityDays { get; set; } = 5475;
}
