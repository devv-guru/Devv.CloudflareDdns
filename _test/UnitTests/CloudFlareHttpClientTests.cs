using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Devv.CloudflareDdns;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Xunit;
using Record=Devv.CloudflareDdns.Record;

public class CloudFlareHttpClientTests
{
    [Fact]
    public async Task UpdateDnsRecordsAsync_WithProxiedTrue_CallsApi()
    {
        // Arrange
        var record = new Record { ZoneId = "zone", DnsRecordId = "dns", Name = "test.com", Proxied = true };
        var options = Options.Create(new CloudFlareOptions { Records = new[] { record } });
        var logger = Mock.Of<ILogger<CloudFlareHttpClient>>();

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{}"),
            });

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new System.Uri("https://api.cloudflare.com")
        };

        var client = new CloudFlareHttpClient(logger, httpClient, options);

        // Act
        await client.UpdateDnsRecordsAsync("1.2.3.4", CancellationToken.None);

        // Assert - verify the API was called
        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task UpdateDnsRecordsAsync_WithProxiedFalse_CallsApi()
    {
        // Arrange
        var record = new Record { ZoneId = "zone", DnsRecordId = "dns", Name = "test.com", Proxied = false };
        var options = Options.Create(new CloudFlareOptions { Records = new[] { record } });
        var logger = Mock.Of<ILogger<CloudFlareHttpClient>>();

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{}"),
            });

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new System.Uri("https://api.cloudflare.com")
        };

        var client = new CloudFlareHttpClient(logger, httpClient, options);

        // Act
        await client.UpdateDnsRecordsAsync("1.2.3.4", CancellationToken.None);

        // Assert - verify the API was called
        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task UpdateDnsRecordsAsync_LogsAndCallsApi()
    {
        // Arrange
        var record = new Record { ZoneId = "zone", DnsRecordId = "dns", Name = "test.com" };
        var options = Options.Create(new CloudFlareOptions { Records = new[] { record } });
        var logger = Mock.Of<ILogger<CloudFlareHttpClient>>();

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("1.2.3.4"),
            });

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new System.Uri("https://api.cloudflare.com")
        };

        var client = new CloudFlareHttpClient(logger, httpClient, options);

        // Act & Assert (should not throw)
        await client.UpdateDnsRecordsAsync("1.2.3.4", CancellationToken.None);
    }
}