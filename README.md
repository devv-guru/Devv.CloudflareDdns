
# Cloudflare Dynamic DNS Updater

[![.NET Release & NuGet Publish](https://github.com/devv-guru/Devv.CloudflareDdns/actions/workflows/main.yml/badge.svg)](https://github.com/devv-guru/Devv.CloudflareDdns/actions/workflows/main.yml)
[![NuGet](https://img.shields.io/nuget/v/Devv.CloudflareDdns.svg)](https://www.nuget.org/packages/Devv.CloudflareDdns/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Devv.CloudflareDdns.svg)](https://www.nuget.org/packages/Devv.CloudflareDdns/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A .NET 8 package to dynamically update Cloudflare DNS records based on changes to your public IP address. This package integrates with Cloudflare's API and automates the process of updating DNS records when the public IP changes.

## Features

- Automatically retrieves your public IP address.
- Updates DNS records via Cloudflare API when the public IP changes.
- Configurable via appsettings for ease of use.
- Supports multiple DNS records.
- Optionally creates and renews Cloudflare Origin CA certificates for proxied records.

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
        "Name": "YourRecordName",
        "Proxied": true
      }
    ],
    "OriginCertificates": {
      "Enabled": true,
      "CheckInterval": "12:00:00",
      "RenewBeforeExpiry": "30.00:00:00",
      "ReloadCommand": "nginx -s reload",
      "Certificates": [
        {
          "Hostnames": [ "example.com", "*.example.com" ],
          "CertificatePath": "/certs/example.com/origin.pem",
          "PrivateKeyPath": "/certs/example.com/origin.key",
          "RequestedValidityDays": 5475
        }
      ]
    }
  }
}
```

### Required Settings

- **Email**: Your Cloudflare account email.
- **Key**: Your Cloudflare API key (can be found in your Cloudflare dashboard).
- **ZoneId**: The ID of your Cloudflare zone.
- **DnsRecordId**: The DNS record ID to update.
- **Name**: The DNS record name (e.g., `example.com`).
- **Proxied**: (Optional) Whether to proxy the DNS record through Cloudflare's CDN (defaults to `true`).
- **OriginCertificates**: (Optional) Cloudflare Origin CA certificate management for proxied records.
- **ReloadCommand**: (Optional) command to run after one or more certificates are written.
- **CertificatePath** and **PrivateKeyPath**: Local output paths for the certificate and generated private key.

Cloudflare Origin CA certificates are only trusted by Cloudflare. They are suitable for records proxied through Cloudflare in Full Strict mode. They are not suitable for non-proxied services like Plex, where clients connect directly and require a publicly trusted certificate.

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
  "CloudflareDdns": {
    "Email": "example@example.com",
    "Key": "your-api-key",
    "Records": [
      {
        "ZoneId": "zone-id-here",
        "DnsRecordId": "dns-record-id-here",
        "Name": "example.com",
        "Proxied": true
      }
    ],
    "OriginCertificates": {
      "Enabled": true,
      "Certificates": [
        {
          "Hostnames": [ "example.com", "*.example.com" ],
          "CertificatePath": "/certs/example.com/origin.pem",
          "PrivateKeyPath": "/certs/example.com/origin.key"
        }
      ]
    }
  }
}
```

## License

This project is licensed under the MIT License.

---

For more information, visit [blog.devv.guru](https://blog.devv.guru).
