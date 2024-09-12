using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;


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
                    new AuthenticationHeaderValue("Bearer", configuration["CloudFlare:Key"]);
            });
            
            services.AddHostedService<DynamicDnsWorker>();

            return services;
        }
    }
}
