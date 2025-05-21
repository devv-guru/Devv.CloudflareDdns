namespace Devv.CloudflareDdns;

public class DnsRecord
{
    public DnsRecord() {}

    public DnsRecord(string id, string domain, string publicIp, string comment, string type = "A")
    {
        Id = id;
        Comment = comment;
        Name = domain;
        Content = publicIp;
        Type = type;
        // Tags = new List<string> { "dynamic-dns" };
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
