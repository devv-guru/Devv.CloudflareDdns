using CloudflareDdns.Server.HealthChecks;
using Devv.CloudflareDdns;
using Serilog;
using Serilog.Filters;

namespace CloudflareDdns.Server;

public class Program
{
    public static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();

        try
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenAnyIP(8080);

                if (builder.Environment.IsDevelopment())
                {
                    options.ListenAnyIP(8081, listenOptions =>
                        listenOptions.UseHttps());
                }
            });

            var config = builder.Configuration;

            config.AddEnvironmentVariables();
            builder.Services.AddCloudflareDynamicDns(config);

            builder.Services.AddSerilog((services, lc) =>
                lc.Filter.ByExcluding(Matching.WithProperty<string>("RequestPath", path =>
                        path != null && path.Contains("/_health")))
                    .Enrich.FromLogContext()
                    .WriteTo.Console());

            builder.Services.AddHealthChecks();


            foreach (var keyValuePair in config.AsEnumerable())
            {
                Log.Information("{key} = {value}", keyValuePair.Key, keyValuePair.Value);
            }

            var app = builder.Build();

            app.UseSerilogRequestLogging(options => { options.GetLevel = LogHelper.ExcludeHealthChecks; });

            app.MapHealthChecks("/_health");

            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
