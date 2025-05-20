using System.Threading;
using System.Threading.Tasks;
using Devv.CloudflareDdns;
using Moq;
using Xunit;

public class IPublicIpProviderTests
{
    [Fact]
    public async Task GetPublicIpAsync_CanBeMocked()
    {
        var mock = new Mock<IPublicIpProvider>();
        mock.Setup(x => x.GetPublicIpAsync(It.IsAny<CancellationToken>())).ReturnsAsync("127.0.0.1");

        var ip = await mock.Object.GetPublicIpAsync(CancellationToken.None);

        Assert.Equal("127.0.0.1", ip);
    }
}