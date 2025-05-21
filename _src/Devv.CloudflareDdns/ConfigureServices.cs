using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;


namespace Devv.CloudflareDdns
{
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Polly;
    using Polly.Extensions.Http;

    public static class ConfigureServices
    {
        public static IServiceCollection AddCloudflareDynamicDns(this IServiceCollection services, IConfiguration configuration)
        {
            var section = configuration.GetSection(CloudFlareOptions.SectionName);
            var opts = section.Get<CloudFlareOptions>();

            services.AddOptions<CloudFlareOptions>()
                .Bind(section);

            services.AddHttpClient<ICloudFlareService, CloudFlareHttpClient>((sp, client) =>
                {
                    client.BaseAddress = opts.ApiUrl;
                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", opts.Key);
                }).ConfigurePrimaryHttpMessageHandler(() =>
                    new SocketsHttpHandler
                    {
                        PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                        PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1),
                        KeepAlivePingPolicy = HttpKeepAlivePingPolicy.Always,
                        KeepAlivePingDelay = TimeSpan.FromSeconds(30),
                        KeepAlivePingTimeout = TimeSpan.FromSeconds(15),
                    }
                )
                .AddPolicyHandler((sp, request) =>
                {
                    var logger = sp.GetRequiredService<ILogger<CloudFlareHttpClient>>();

                    return HttpPolicyExtensions
                        .HandleTransientHttpError()
                        .WaitAndRetryAsync(
                        retryCount: 3,
                        sleepDurationProvider: attempt =>
                            TimeSpan.FromSeconds(Math.Pow(2, attempt)),// 2s, 4s, 8s
                        onRetry: (outcome, timespan, retryAttempt, context) =>
                        {
                            logger.LogWarning(
                            "Attempt {Attempt} failed (status {Status}), retrying in {Delay}s",
                            retryAttempt,
                            outcome.Exception is null
                                ? ((int)outcome.Result.StatusCode).ToString()
                                : outcome.Exception.Message,
                            timespan.TotalSeconds
                            );
                        }
                        );
                });

            services.AddHttpClient<IPublicIpProvider, PublicIpProvider>((sp, client) => 
            { client.BaseAddress = new Uri("https://api.ipify.org"); })
            .ConfigurePrimaryHttpMessageHandler(() =>
                new SocketsHttpHandler
                {
                    PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                    PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1),
                    KeepAlivePingPolicy = HttpKeepAlivePingPolicy.Always,
                    KeepAlivePingDelay = TimeSpan.FromSeconds(30),
                    KeepAlivePingTimeout = TimeSpan.FromSeconds(15),
                }
            )
            .AddPolicyHandler((sp, request) =>
            {
                var logger = sp.GetRequiredService<ILogger<CloudFlareHttpClient>>();

                return HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: attempt =>
                        TimeSpan.FromSeconds(Math.Pow(2, attempt)),// 2s, 4s, 8s
                    onRetry: (outcome, timespan, retryAttempt, context) =>
                    {
                        logger.LogWarning(
                        "Attempt {Attempt} failed (status {Status}), retrying in {Delay}s",
                        retryAttempt,
                        outcome.Exception is null
                            ? ((int)outcome.Result.StatusCode).ToString()
                            : outcome.Exception.Message,
                        timespan.TotalSeconds
                        );
                    }
                    );
            });
            
            services.AddHostedService<DynamicDnsWorker>();

            return services;
        }
    }
}
