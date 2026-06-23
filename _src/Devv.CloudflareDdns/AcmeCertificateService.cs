using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Devv.CloudflareDdns;

public sealed class AcmeCertificateService : IAcmeCertificateService
{
    private readonly IAcmeCertificateIssuer _issuer;
    private readonly IOptions<CloudFlareOptions> _options;
    private readonly IProcessRunner _processRunner;
    private readonly ILogger<AcmeCertificateService> _logger;

    public AcmeCertificateService(
        IAcmeCertificateIssuer issuer,
        IOptions<CloudFlareOptions> options,
        IProcessRunner processRunner,
        ILogger<AcmeCertificateService> logger)
    {
        _issuer = issuer;
        _options = options;
        _processRunner = processRunner;
        _logger = logger;
    }

    public async Task EnsureCertificatesAsync(CancellationToken cancellationToken)
    {
        var options = _options.Value.AcmeCertificates;
        if (!options.Enabled)
        {
            _logger.LogDebug("ACME certificate management is disabled");
            return;
        }

        if (!HasRequiredGlobalOptions(options))
        {
            _logger.LogWarning("Skipping ACME certificate management; Email and AccountKeyPath are required");
            return;
        }

        var changed = false;
        foreach (var certificate in options.Certificates)
        {
            if (!IsValid(certificate))
            {
                _logger.LogWarning("Skipping invalid ACME certificate config");
                continue;
            }

            if (!NeedsRenewal(certificate, options.RenewBeforeExpiry))
            {
                _logger.LogInformation(
                    "ACME certificate for {Hostnames} is still valid",
                    string.Join(", ", certificate.Hostnames));
                continue;
            }

            _logger.LogInformation(
                "Requesting ACME certificate for {Hostnames}",
                string.Join(", ", certificate.Hostnames));

            var issued = await _issuer.IssueCertificateAsync(certificate, options, cancellationToken);
            WriteAtomically(certificate.PrivateKeyPath!, issued.PrivateKeyPem);
            WriteAtomically(certificate.CertificatePath!, issued.CertificatePem);
            changed = true;
        }

        if (changed && !string.IsNullOrWhiteSpace(options.ReloadCommand))
        {
            _logger.LogInformation("Running ACME certificate reload command");
            await _processRunner.RunAsync(options.ReloadCommand, cancellationToken);
        }
    }

    private static bool HasRequiredGlobalOptions(AcmeCertificateOptions options)
    {
        return !string.IsNullOrWhiteSpace(options.Email)
               && !string.IsNullOrWhiteSpace(options.AccountKeyPath);
    }

    private static bool IsValid(AcmeCertificate certificate)
    {
        return !string.IsNullOrWhiteSpace(certificate.ZoneId)
               && certificate.Hostnames.Length > 0
               && certificate.Hostnames.All(hostname => !string.IsNullOrWhiteSpace(hostname))
               && !string.IsNullOrWhiteSpace(certificate.CertificatePath)
               && !string.IsNullOrWhiteSpace(certificate.PrivateKeyPath);
    }

    private bool NeedsRenewal(AcmeCertificate certificate, TimeSpan renewBeforeExpiry)
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
                "Existing ACME certificate at {CertificatePath} could not be read; it will be replaced",
                certificate.CertificatePath);
            return true;
        }
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
}
