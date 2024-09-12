
# Cloudflare Dynamic DNS Updater

A .NET 8 package to dynamically update Cloudflare DNS records based on changes to your public IP address. This package integrates with Cloudflare's API and automates the process of updating DNS records when the public IP changes.

## Features

- Automatically retrieves your public IP address.
- Updates DNS records via Cloudflare API when the public IP changes.
- Configurable via appsettings for ease of use.
- Supports multiple DNS records.

## Installation

You can install the package via NuGet:

```bash
dotnet add package Devv.CloudflareDdns
```

## Configuration

Add the required settings to your `appsettings.json` file:

```json
{
  "CloudFlareDdns": {
    "Email": "YourCloudFlareEmail",
    "Key": "YourCloudFlareKey",
    "Records": [
      {
        "ZoneId": "YourZoneId",
        "DnsRecordId": "YourRecordId",
        "Name": "YourRecordName"
      }
    ]
  }
}
```

### Required Settings

- **Email**: Your Cloudflare account email.
- **Key**: Your Cloudflare API key (can be found in your Cloudflare dashboard).
- **ZoneId**: The ID of your Cloudflare zone.
- **DnsRecordId**: The DNS record ID to update.
- **Name**: The DNS record name (e.g., `example.com`).

## Usage

1. In your `Program.cs` or `Startup.cs`, add the following to configure the service:

```csharp
public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    services.AddCloudflareDynamicDns(configuration);
}
```

2. The package will automatically run a background service that checks for changes to your public IP every 60 seconds and updates the DNS records if needed.

## Example

Below is a basic example of `appsettings.json` configuration:

```json
{
  "CloudFlare": {
    "Email": "example@example.com",
    "Key": "your-api-key",
    "Records": [
      {
        "ZoneId": "zone-id-here",
        "DnsRecordId": "dns-record-id-here",
        "Name": "example.com"
      }
    ]
  }
}
```

## License

This project is licensed under the MIT License.

---

For more information, visit [blog.devv.guru](https://blog.devv.guru).
