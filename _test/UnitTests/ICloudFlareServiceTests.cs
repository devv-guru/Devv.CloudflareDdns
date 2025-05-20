using System.Threading;
using System.Threading.Tasks;
using Devv.CloudflareDdns;
using Moq;
using Xunit;

public class ICloudFlareServiceTests
{
    [Fact]
    public async Task UpdateDnsRecordsAsync_CanBeMocked()
    {
        var mock = new Mock<ICloudFlareService>();
        mock.Setup(x => x.UpdateDnsRecordsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await mock.Object.UpdateDnsRecordsAsync("1.2.3.4", CancellationToken.None);

        mock.Verify(x => x.UpdateDnsRecordsAsync("1.2.3.4", It.IsAny<CancellationToken>()), Times.Once);
    }
}