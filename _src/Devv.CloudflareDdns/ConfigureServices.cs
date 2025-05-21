using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;


namespace Devv.CloudflareDdns
{
    using Microsoft.Extensions.Options;

    public static class ConfigureServices
    {
        public static IServiceCollection AddCloudflareDynamicDns(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<CloudFlareOptions>()
                .Bind (configuration.GetSection(CloudFlareOptions.SectionName));

            services.AddHttpClient<ICloudFlareService, CloudFlareHttpClient>((sp, client) =>
            {
                var opts = sp
                    .GetRequiredService<IOptions<CloudFlareOptions>>()
                    .Value;
                client.BaseAddress = opts.ApiUrl;
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", opts.Key);
            });

            services.AddScoped<IPublicIpProvider, CloudFlareHttpClient>();
            services.AddHostedService<DynamicDnsWorker>();

            return services;
        }
    }
}
