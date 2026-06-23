using Certes;
using Certes.Acme.Resource;
using Microsoft.Extensions.Logging;

namespace Devv.CloudflareDdns;

public sealed class CertesAcmeCertificateIssuer : IAcmeCertificateIssuer
{
    private readonly ICloudflareDnsChallengeService _dnsChallengeService;
    private readonly ILogger<CertesAcmeCertificateIssuer> _logger;

    public CertesAcmeCertificateIssuer(
        ICloudflareDnsChallengeService dnsChallengeService,
        ILogger<CertesAcmeCertificateIssuer> logger)
    {
        _dnsChallengeService = dnsChallengeService;
        _logger = logger;
    }

    public async Task<IssuedAcmeCertificate> IssueCertificateAsync(
        AcmeCertificate certificate,
        AcmeCertificateOptions options,
        CancellationToken cancellationToken)
    {
        var accountKey = LoadOrCreateAccountKey(options.AccountKeyPath!);
        var acme = new AcmeContext(options.DirectoryUri, accountKey);
        await acme.NewAccount(new[] { $"mailto:{options.Email}" }, termsOfServiceAgreed: true);

        var order = await acme.NewOrder(certificate.Hostnames);
        var challengeRecords = new List<ChallengeRecord>();

        try
        {
            foreach (var authorization in await order.Authorizations())
            {
                var resource = await authorization.Resource();
                var challenge = await authorization.Dns()
                    ?? throw new InvalidOperationException(
                        $"ACME DNS-01 challenge is not available for {resource.Identifier.Value}");

                var txtName = GetDnsChallengeName(resource.Identifier.Value);
                var txtValue = acme.AccountKey.DnsTxt(challenge.Token);

                _logger.LogInformation("Creating ACME DNS challenge {Name}", txtName);
                var recordId = await _dnsChallengeService.CreateTxtRecordAsync(
                    certificate.ZoneId,
                    txtName,
                    txtValue,
                    cancellationToken);

                challengeRecords.Add(new ChallengeRecord(certificate.ZoneId, recordId));
            }

            await Task.Delay(options.DnsPropagationDelay, cancellationToken);

            foreach (var authorization in await order.Authorizations())
            {
                var challenge = await authorization.Dns()
                    ?? throw new InvalidOperationException("ACME DNS-01 challenge disappeared before validation");

                await challenge.Validate();
                await WaitForAuthorizationAsync(authorization, options, cancellationToken);
            }

            var certificateKey = KeyFactory.NewKey(KeyAlgorithm.RS256);
            var chain = await order.Generate(
                new CsrInfo { CommonName = certificate.Hostnames[0] },
                certificateKey,
                certificate.PreferredChain);

            var certificatePem = EnsureTrailingNewline(chain.Certificate.ToPem())
                                 + string.Concat(chain.Issuers.Select(issuer => EnsureTrailingNewline(issuer.ToPem())));

            return new IssuedAcmeCertificate(certificatePem, certificateKey.ToPem());
        }
        finally
        {
            foreach (var record in challengeRecords)
            {
                await _dnsChallengeService.DeleteTxtRecordAsync(
                    record.ZoneId,
                    record.RecordId,
                    cancellationToken);
            }
        }
    }

    private static IKey LoadOrCreateAccountKey(string path)
    {
        if (File.Exists(path))
        {
            return KeyFactory.FromPem(File.ReadAllText(path));
        }

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            System.IO.Directory.CreateDirectory(directory);
        }

        var key = KeyFactory.NewKey(KeyAlgorithm.RS256);
        File.WriteAllText(path, key.ToPem());
        return key;
    }

    private static async Task WaitForAuthorizationAsync(
        Certes.Acme.IAuthorizationContext authorization,
        AcmeCertificateOptions options,
        CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow.Add(options.ValidationTimeout);
        while (DateTimeOffset.UtcNow < deadline)
        {
            var resource = await authorization.Resource();
            if (resource.Status == AuthorizationStatus.Valid)
            {
                return;
            }

            if (resource.Status == AuthorizationStatus.Invalid)
            {
                throw new InvalidOperationException(
                    $"ACME authorization failed for {resource.Identifier.Value}");
            }

            await Task.Delay(options.PollInterval, cancellationToken);
        }

        throw new TimeoutException("Timed out waiting for ACME DNS-01 authorization");
    }

    private static string GetDnsChallengeName(string identifier)
    {
        return "_acme-challenge." + identifier.TrimStart('*').TrimStart('.');
    }

    private static string EnsureTrailingNewline(string value)
    {
        return value.EndsWith(Environment.NewLine, StringComparison.Ordinal)
            ? value
            : value + Environment.NewLine;
    }

    private sealed record ChallengeRecord(string ZoneId, string RecordId);
}
