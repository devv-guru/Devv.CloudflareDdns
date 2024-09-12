namespace CloudflareDdns.Package;

public class DnsRecord
{
    public DnsRecord(string id, string domain, string publicIp, string comment, string type = "A")
    {
        Id = id;
        Comment = comment;
        Name = domain;
        Content = publicIp;
        Type = type;
        // Tags = new List<string> { "dynamic-dns" };
    }

    public string Content { get; set; }
    public string Name { get; set; }
    public bool Proxied { get; set; } = true;
    public string Type { get; set; }
    public string Comment { get; set; }
    public string Id { get; set; }
    public List<string> Tags { get; set; } = new List<string>();
    public int Ttl { get; set; } = 1;
}