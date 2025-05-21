# syntax=docker/dockerfile:1.4

###############################################################################
# 1) Restore + build
###############################################################################
FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim AS build
ARG BUILD_CONFIGURATION=Release

WORKDIR /src

# Copy each csproj into its matching folder for restore
COPY _src/CloudflareDdns.Server/CloudflareDdns.Server.csproj _src/CloudflareDdns.Server/
COPY _src/Devv.CloudflareDdns/Devv.CloudflareDdns.csproj _src/Devv.CloudflareDdns/

# Restore Server (including linux-x64 RID so assets.json has net8.0/linux-x64)
RUN dotnet restore \
      _src/CloudflareDdns.Server/CloudflareDdns.Server.csproj \
      -r linux-x64 \
      /p:PublishReadyToRun=true

# Copy the rest of your source
COPY . .

# Build into /app/build
RUN dotnet build \
      _src/CloudflareDdns.Server/CloudflareDdns.Server.csproj \
      -c $BUILD_CONFIGURATION \
      -r linux-x64 \
      -o /app/build \
      --no-restore

###############################################################################
# 2) Publish as a single, trimmed, self-contained file
###############################################################################
FROM build AS publish
ARG BUILD_CONFIGURATION=Release

RUN dotnet publish \
      _src/CloudflareDdns.Server/CloudflareDdns.Server.csproj \
      -c $BUILD_CONFIGURATION \
      -r linux-x64 \
      --self-contained true \
      /p:PublishSingleFile=true \
      /p:PublishTrimmed=true \      
      -o /app/publish

###############################################################################
# 3) Final: minimal Debian “Bookworm-slim” runtime-deps + non-root user
###############################################################################
FROM mcr.microsoft.com/dotnet/runtime-deps:8.0-bookworm-slim AS final

ARG APP_UID=1000

# Create 'app' user only if it doesn't exist
RUN if ! id app >/dev/null 2>&1; then \
      useradd \
        --uid $APP_UID \
        --home-dir /app \
        --no-log-init \
        --create-home \
        --shell /sbin/nologin \
        app; \
    fi

WORKDIR /app
USER app

# Copy the published single-file output
COPY --from=publish /app/publish ./

# Configure Kestrel ports
ENV ASPNETCORE_URLS="http://+:8080;"
EXPOSE 8080

ENTRYPOINT ["./CloudflareDdns.Server"]