using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;


namespace Devv.CloudflareDdns
{
    public static class ConfigureServices
    {
        public static IServiceCollection AddCloudflareDynamicDns(this IServiceCollection services, IConfiguration configuration)
        {            
            services.AddOptions<CloudFlareOptions>()
                .BindConfiguration(CloudFlareOptions.SectionName);

            services.AddHttpClient<CloudFlareHttpClient>(options =>
            {
                options.BaseAddress = new Uri("https://api.cloudflare.com");
                options.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", configuration["CloudflareDdns:Key"]);
            });
            
            services.AddScoped<ICloudFlareService, CloudFlareHttpClient>();
            services.AddScoped<IPublicIpProvider, CloudFlareHttpClient>();
            services.AddHostedService<DynamicDnsWorker>();

            return services;
        }
    }
}
