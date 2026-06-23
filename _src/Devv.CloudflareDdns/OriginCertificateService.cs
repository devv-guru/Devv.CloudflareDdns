using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Devv.CloudflareDdns;

public sealed class OriginCertificateService : IOriginCertificateService
{
    private const string PrivateKeyPemLabel = "PRIVATE KEY";
    private const string CertificateRequestPemLabel = "CERTIFICATE REQUEST";

    private readonly IOriginCertificateApi _originCertificateApi;
    private readonly IOptions<CloudFlareOptions> _options;
    private readonly IProcessRunner _processRunner;
    private readonly ILogger<OriginCertificateService> _logger;

    public OriginCertificateService(
        IOriginCertificateApi originCertificateApi,
        IOptions<CloudFlareOptions> options,
        IProcessRunner processRunner,
        ILogger<OriginCertificateService> logger)
    {
        _originCertificateApi = originCertificateApi;
        _options = options;
        _processRunner = processRunner;
        _logger = logger;
    }

    public async Task EnsureCertificatesAsync(CancellationToken cancellationToken)
    {
        var options = _options.Value.OriginCertificates;
        if (!options.Enabled)
        {
            _logger.LogDebug("Cloudflare Origin CA certificate management is disabled");
            return;
        }

        var changed = false;
        foreach (var certificate in options.Certificates)
        {
            if (!IsValid(certificate))
            {
                _logger.LogWarning("Skipping invalid Origin CA certificate config");
                continue;
            }

            if (!NeedsRenewal(certificate, options.RenewBeforeExpiry))
            {
                _logger.LogInformation(
                    "Origin CA certificate for {Hostnames} is still valid",
                    string.Join(", ", certificate.Hostnames));
                continue;
            }

            _logger.LogInformation(
                "Requesting Cloudflare Origin CA certificate for {Hostnames}",
                string.Join(", ", certificate.Hostnames));

            var generated = GenerateCertificateRequest(certificate.Hostnames);
            var result = await _originCertificateApi.CreateOriginCertificateAsync(
                certificate,
                generated.CsrPem,
                cancellationToken);

            WriteAtomically(certificate.PrivateKeyPath!, generated.PrivateKeyPem);
            WriteAtomically(certificate.CertificatePath!, EnsureTrailingNewline(result.Certificate!));

            _logger.LogInformation(
                "Wrote Origin CA certificate for {Hostnames}. Cloudflare expiry: {ExpiresOn}",
                string.Join(", ", certificate.Hostnames),
                result.ExpiresOn);

            changed = true;
        }

        if (changed && !string.IsNullOrWhiteSpace(options.ReloadCommand))
        {
            _logger.LogInformation("Running certificate reload command");
            await _processRunner.RunAsync(options.ReloadCommand, cancellationToken);
        }
    }

    private static bool IsValid(OriginCertificate certificate)
    {
        return certificate.Hostnames.Length > 0
               && certificate.Hostnames.All(hostname => !string.IsNullOrWhiteSpace(hostname))
               && !string.IsNullOrWhiteSpace(certificate.CertificatePath)
               && !string.IsNullOrWhiteSpace(certificate.PrivateKeyPath)
               && certificate.RequestedValidityDays > 0;
    }

    private bool NeedsRenewal(OriginCertificate certificate, TimeSpan renewBeforeExpiry)
    {
        if (!File.Exists(certificate.CertificatePath) || !File.Exists(certificate.PrivateKeyPath))
        {
            return true;
        }

        try
        {
            var certPem = File.ReadAllText(certificate.CertificatePath);
            using var cert = X509Certificate2.CreateFromPem(certPem);
            return cert.NotAfter.ToUniversalTime() <= DateTime.UtcNow.Add(renewBeforeExpiry);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Existing Origin CA certificate at {CertificatePath} could not be read; it will be replaced",
                certificate.CertificatePath);
            return true;
        }
    }

    private static GeneratedCertificateRequest GenerateCertificateRequest(IReadOnlyList<string> hostnames)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            $"CN={hostnames[0]}",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        var subjectAlternativeNames = new SubjectAlternativeNameBuilder();
        foreach (var hostname in hostnames)
        {
            subjectAlternativeNames.AddDnsName(hostname);
        }

        request.CertificateExtensions.Add(subjectAlternativeNames.Build());

        var csrPem = ToPem(CertificateRequestPemLabel, request.CreateSigningRequest());
        var privateKeyPem = ToPem(PrivateKeyPemLabel, rsa.ExportPkcs8PrivateKey());

        return new GeneratedCertificateRequest(csrPem, privateKeyPem);
    }

    private static void WriteAtomically(string path, string content)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempPath = $"{path}.{Guid.NewGuid():N}.tmp";
        File.WriteAllText(tempPath, content);
        File.Move(tempPath, path, overwrite: true);
    }

    private static string ToPem(string label, byte[] der)
    {
        var base64 = Convert.ToBase64String(der);
        var writer = new StringWriter();
        writer.WriteLine($"-----BEGIN {label}-----");

        for (var i = 0; i < base64.Length; i += 64)
        {
            writer.WriteLine(base64.Substring(i, Math.Min(64, base64.Length - i)));
        }

        writer.WriteLine($"-----END {label}-----");
        return writer.ToString();
    }

    private static string EnsureTrailingNewline(string value)
    {
        return value.EndsWith(Environment.NewLine, StringComparison.Ordinal)
            ? value
            : value + Environment.NewLine;
    }

    private sealed record GeneratedCertificateRequest(string CsrPem, string PrivateKeyPem);
}
