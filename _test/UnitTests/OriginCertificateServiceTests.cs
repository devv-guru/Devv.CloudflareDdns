using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Devv.CloudflareDdns;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

public class OriginCertificateServiceTests
{
    [Fact]
    public async Task EnsureCertificatesAsync_WhenCertificateExistsAndIsValid_DoesNotCallApi()
    {
        using var temp = new TempDirectory();
        var certificatePath = Path.Combine(temp.Path, "origin.pem");
        var privateKeyPath = Path.Combine(temp.Path, "origin.key");

        File.WriteAllText(certificatePath, CreateCertificatePem(DateTimeOffset.UtcNow.AddDays(90)));
        File.WriteAllText(privateKeyPath, "existing-key");

        var api = new Mock<IOriginCertificateApi>();
        var runner = new Mock<IProcessRunner>();
        var service = CreateService(api, runner, certificatePath, privateKeyPath);

        await service.EnsureCertificatesAsync(CancellationToken.None);

        api.Verify(
            x => x.CreateOriginCertificateAsync(
                It.IsAny<OriginCertificate>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        runner.Verify(x => x.RunAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EnsureCertificatesAsync_WhenCertificateIsMissing_CreatesAndWritesCertificate()
    {
        using var temp = new TempDirectory();
        var certificatePath = Path.Combine(temp.Path, "origin.pem");
        var privateKeyPath = Path.Combine(temp.Path, "origin.key");
        var issuedCertificate = CreateCertificatePem(DateTimeOffset.UtcNow.AddDays(365));

        var api = new Mock<IOriginCertificateApi>();
        api.Setup(x => x.CreateOriginCertificateAsync(
                It.IsAny<OriginCertificate>(),
                It.Is<string>(csr => csr.Contains("BEGIN CERTIFICATE REQUEST")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OriginCertificateResult
            {
                Id = "cert-id",
                Certificate = issuedCertificate,
                ExpiresOn = DateTimeOffset.UtcNow.AddDays(365)
            });

        var runner = new Mock<IProcessRunner>();
        var service = CreateService(api, runner, certificatePath, privateKeyPath, "nginx -s reload");

        await service.EnsureCertificatesAsync(CancellationToken.None);

        Assert.Equal(issuedCertificate.Trim(), File.ReadAllText(certificatePath).Trim());
        Assert.Contains("BEGIN PRIVATE KEY", File.ReadAllText(privateKeyPath));
        runner.Verify(x => x.RunAsync("nginx -s reload", It.IsAny<CancellationToken>()), Times.Once);
    }

    private static OriginCertificateService CreateService(
        Mock<IOriginCertificateApi> api,
        Mock<IProcessRunner> runner,
        string certificatePath,
        string privateKeyPath,
        string? reloadCommand = null)
    {
        var options = Options.Create(new CloudFlareOptions
        {
            OriginCertificates = new OriginCertificateOptions
            {
                Enabled = true,
                RenewBeforeExpiry = TimeSpan.FromDays(30),
                ReloadCommand = reloadCommand,
                Certificates =
                [
                    new OriginCertificate
                    {
                        Hostnames = ["example.com", "*.example.com"],
                        CertificatePath = certificatePath,
                        PrivateKeyPath = privateKeyPath
                    }
                ]
            }
        });

        return new OriginCertificateService(
            api.Object,
            options,
            runner.Object,
            NullLogger<OriginCertificateService>.Instance);
    }

    private static string CreateCertificatePem(DateTimeOffset expires)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=example.com",
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
