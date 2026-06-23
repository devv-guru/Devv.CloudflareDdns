using System.Text.Json.Serialization;

namespace Devv.CloudflareDdns;

public sealed class OriginCertificateRequest
{
    public OriginCertificateRequest(
        IReadOnlyCollection<string> hostnames,
        int requestedValidity,
        string requestType,
        string csr)
    {
        Hostnames = hostnames;
        RequestedValidity = requestedValidity;
        RequestType = requestType;
        Csr = csr;
    }

    [JsonPropertyName("hostnames")]
    public IReadOnlyCollection<string> Hostnames { get; }

    [JsonPropertyName("requested_validity")]
    public int RequestedValidity { get; }

    [JsonPropertyName("request_type")]
    public string RequestType { get; }

    [JsonPropertyName("csr")]
    public string Csr { get; }
}

public sealed class CloudflareApiResponse<T>
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("errors")]
    public CloudflareApiMessage[] Errors { get; set; } = Array.Empty<CloudflareApiMessage>();

    [JsonPropertyName("result")]
    public T? Result { get; set; }
}

public sealed class CloudflareApiMessage
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

public sealed class OriginCertificateResult
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("certificate")]
    public string? Certificate { get; set; }

    [JsonPropertyName("expires_on")]
    public DateTimeOffset ExpiresOn { get; set; }
}
