namespace Devv.CloudflareDdns;

using System.Text.Json.Serialization;

public class DnsRecord
{
    public DnsRecord()
    {
    }

    [JsonConstructor]
    public DnsRecord(string id, string domain, string publicIp, string comment, bool proxied = true, string type = "A")
    {
        Id = id;
        Comment = comment;
        Name = domain;
        Content = publicIp;
        Type = type;
        Proxied = proxied;
    }

    public string Id { get; set; } = default!;

    public string Name { get; set; } = default!;

    public string Content { get; set; } = default!;

    public string Comment { get; set; } = default!;

    public string Type { get; set; } = "A";

    public bool Proxied { get; set; } = true;

    public int Ttl { get; set; } = 1;

    public List<string> Tags { get; set; } = new();
}

[JsonSerializable(typeof(DnsRecord))]
[JsonSerializable(typeof(OriginCertificateRequest))]
[JsonSerializable(typeof(CloudflareApiResponse<OriginCertificateResult>))]
[JsonSerializable(typeof(CloudflareApiResponse<DnsChallengeRecordResult>))]
internal partial class CustomJsonContext : JsonSerializerContext
{
}
