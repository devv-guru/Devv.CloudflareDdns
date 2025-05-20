using System.Threading;
using System.Threading.Tasks;
using Devv.CloudflareDdns;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

public class DynamicDnsWorkerTests
{
    [Fact]
    public async Task ExecuteAsync_UpdatesDnsWhenIpChanges()
    {
        // Arrange
        var ipProvider = new Mock<IPublicIpProvider>();
        var cloudFlareService = new Mock<ICloudFlareService>();
        var logger = Mock.Of<ILogger<DynamicDnsWorker>>();

        ipProvider.SetupSequence(x => x.GetPublicIpAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("1.2.3.4")
            .ReturnsAsync("5.6.7.8");

        var worker = new DynamicDnsWorker(ipProvider.Object, cloudFlareService.Object, logger);

        var cts = new CancellationTokenSource();
        cts.CancelAfter(200); // Short run

        // Act
        await worker.StartAsync(cts.Token);

        // Assert
        cloudFlareService.Verify(x => x.UpdateDnsRecordsAsync("1.2.3.4", It.IsAny<CancellationToken>()), Times.Once);
    }
}