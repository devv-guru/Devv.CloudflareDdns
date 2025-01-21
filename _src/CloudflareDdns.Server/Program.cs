using Azure.Identity;
using Devv.CloudflareDdns;
using Serilog;

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
            var config = builder.Configuration;

            var clientId = config["AZURE_CLIENT_ID"];
            var secret = config["AZURE_CLIENT_SECRET"];
            var tenantId = config["AZURE_TENANT_ID"];
            var vaultUrl = config["VAULT_URL"];

            if (string.IsNullOrEmpty(clientId))
            {
                throw new InvalidOperationException("VAULT_CLIENT_ID is not set!");
            }

            if (string.IsNullOrEmpty(secret))
            {
                throw new InvalidOperationException("VAULT_CLIENT_SECRET is not set!");
            }

            if (string.IsNullOrEmpty(vaultUrl))
            {
                throw new InvalidOperationException("VAULT_URL is not set!");
            }

            builder.Configuration.AddAzureKeyVault(new Uri(vaultUrl), new DefaultAzureCredential());

            builder.Services.AddSerilog((services, lc) =>
                lc.ReadFrom.Configuration(builder.Configuration)
                .Enrich.FromLogContext()
                .WriteTo.Console());

            builder.Services.AddAuthorization();

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddCloudflareDynamicDns(config);

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

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