using Azure.Identity;
using Devv.CloudflareDdns;

namespace CloudflareDdns.Server;

public class Program
{
    public static void Main(string[] args)
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
}