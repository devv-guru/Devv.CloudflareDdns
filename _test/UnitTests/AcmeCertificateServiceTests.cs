using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Devv.CloudflareDdns;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

public class AcmeCertificateServiceTests
{
    [Fact]
    public async Task EnsureCertificatesAsync_WhenCertificateExistsAndIsValid_DoesNotCallIssuer()
    {
        using var temp = new TempDirectory();
        var certificatePath = Path.Combine(temp.Path, "fullchain.pem");
        var privateKeyPath = Path.Combine(temp.Path, "privkey.pem");

        File.WriteAllText(certificatePath, CreateCertificatePem(DateTimeOffset.UtcNow.AddDays(90)));
        File.WriteAllText(privateKeyPath, "existing-key");

        var issuer = new Mock<IAcmeCertificateIssuer>();
        var runner = new Mock<IProcessRunner>();
        var service = CreateService(issuer, runner, certificatePath, privateKeyPath);

        await service.EnsureCertificatesAsync(CancellationToken.None);

        issuer.Verify(
            x => x.IssueCertificateAsync(
                It.IsAny<AcmeCertificate>(),
                It.IsAny<AcmeCertificateOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        runner.Verify(x => x.RunAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EnsureCertificatesAsync_WhenCertificateIsMissing_IssuesAndWritesCertificate()
    {
        using var temp = new TempDirectory();
        var certificatePath = Path.Combine(temp.Path, "fullchain.pem");
        var privateKeyPath = Path.Combine(temp.Path, "privkey.pem");
        var issuedCertificate = CreateCertificatePem(DateTimeOffset.UtcNow.AddDays(90));
        const string issuedKey = "-----BEGIN PRIVATE KEY-----\ntest\n-----END PRIVATE KEY-----\n";

        var issuer = new Mock<IAcmeCertificateIssuer>();
        issuer.Setup(x => x.IssueCertificateAsync(
                It.IsAny<AcmeCertificate>(),
                It.IsAny<AcmeCertificateOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IssuedAcmeCertificate(issuedCertificate, issuedKey));

        var runner = new Mock<IProcessRunner>();
        var service = CreateService(issuer, runner, certificatePath, privateKeyPath, "nginx -s reload");

        await service.EnsureCertificatesAsync(CancellationToken.None);

        Assert.Equal(issuedCertificate, File.ReadAllText(certificatePath));
        Assert.Equal(issuedKey, File.ReadAllText(privateKeyPath));
        runner.Verify(x => x.RunAsync("nginx -s reload", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EnsureCertificatesAsync_WhenHostnameIsEmpty_SkipsCertificate()
    {
        using var temp = new TempDirectory();
        var certificatePath = Path.Combine(temp.Path, "fullchain.pem");
        var privateKeyPath = Path.Combine(temp.Path, "privkey.pem");

        var issuer = new Mock<IAcmeCertificateIssuer>();
        var runner = new Mock<IProcessRunner>();
        var service = CreateService(issuer, runner, certificatePath, privateKeyPath, hostnames: [""]);

        await service.EnsureCertificatesAsync(CancellationToken.None);

        issuer.Verify(
            x => x.IssueCertificateAsync(
                It.IsAny<AcmeCertificate>(),
                It.IsAny<AcmeCertificateOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static AcmeCertificateService CreateService(
        Mock<IAcmeCertificateIssuer> issuer,
        Mock<IProcessRunner> runner,
        string certificatePath,
        string privateKeyPath,
        string? reloadCommand = null,
        string[]? hostnames = null)
    {
        var options = Options.Create(new CloudFlareOptions
        {
            AcmeCertificates = new AcmeCertificateOptions
            {
                Enabled = true,
                Email = "admin@example.com",
                AccountKeyPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "account.key"),
                RenewBeforeExpiry = TimeSpan.FromDays(30),
                ReloadCommand = reloadCommand,
                Certificates =
                [
                    new AcmeCertificate
                    {
                        ZoneId = "zone-id",
                        Hostnames = hostnames ?? ["plex.example.com"],
                        CertificatePath = certificatePath,
                        PrivateKeyPath = privateKeyPath
                    }
                ]
            }
        });

        return new AcmeCertificateService(
            issuer.Object,
            options,
            runner.Object,
            NullLogger<AcmeCertificateService>.Instance);
    }

    private static string CreateCertificatePem(DateTimeOffset expires)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=plex.example.com",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        using var certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            expires);

        return certificate.ExportCertificatePem();
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
