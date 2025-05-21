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
            var section = configuration.GetSection(CloudFlareOptions.SectionName);
            var opts    = section.Get<CloudFlareOptions>();
            
            Console.WriteLine($"Records array is {(opts.Records == null ? "null" : opts.Records.Length.ToString())}");
            if (opts.Records is not null)
            {
                for (var i = 0; i < opts.Records.Length; i++)
                    Console.WriteLine($" • [{i}] {opts.Records[i].Name} ({opts.Records[i].ZoneId})");
            }
            
            services.Configure<CloudFlareOptions>
                (configuration.GetSection(CloudFlareOptions.SectionName));

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
