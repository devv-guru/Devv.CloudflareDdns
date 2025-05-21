# syntax=docker/dockerfile:1.4

###############################################################################
# 1) Restore + build
###############################################################################
FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim AS build
ARG BUILD_CONFIGURATION=Release

WORKDIR /src

# Copy project files
COPY CloudflareDdns.Server/CloudflareDdns.Server.csproj CloudflareDdns.Server/
COPY Devv.CloudflareDdns/Devv.CloudflareDdns.csproj Devv.CloudflareDdns/

# Restore including the linux-x64 RID so project.assets.json has the right target
RUN dotnet restore \
      CloudflareDdns.Server/CloudflareDdns.Server.csproj \
      -r linux-x64

# Copy the rest of your source
COPY . .

# Build for linux-x64 (no restore) into /app/build
RUN dotnet build \
      CloudflareDdns.Server/CloudflareDdns.Server.csproj \
      -c $BUILD_CONFIGURATION \
      -r linux-x64 \
      -o /app/build \
      --no-restore

###############################################################################
# 2) Publish as a single, trimmed, self-contained file
###############################################################################
FROM build AS publish
ARG BUILD_CONFIGURATION=Release

# Publish (no build) now that rid-specific assets exist
RUN dotnet publish \
      CloudflareDdns.Server/CloudflareDdns.Server.csproj \
      -c $BUILD_CONFIGURATION \
      -r linux-x64 \
      --self-contained true \
      /p:PublishSingleFile=true \
      /p:PublishTrimmed=true \
      /p:PublishReadyToRun=true \
      -o /app/publish \
      --no-build

###############################################################################
# 3) Final: minimal Debian “Bookworm-slim” runtime-deps + safe non-root user
###############################################################################
FROM mcr.microsoft.com/dotnet/runtime-deps:8.0-bookworm-slim AS final
ARG APP_UID=1000

# Only create the 'app' user if it doesn't already exist
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

# Copy the single-file publish output
COPY --from=publish /app/publish ./

# Tell Kestrel to listen on 8080 and 8081
ENV ASPNETCORE_URLS="http://+:8080;https://+:8081"
EXPOSE 8080 8081

ENTRYPOINT ["./CloudflareDdns.Server"]
