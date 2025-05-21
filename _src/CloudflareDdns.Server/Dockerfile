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

# Restore Server (which will pull in Devv.CloudflareDdns as a project reference)
RUN dotnet restore _src/CloudflareDdns.Server/CloudflareDdns.Server.csproj

# Copy the rest of your source
COPY . .

# Build into /app/build
RUN dotnet build \
      _src/CloudflareDdns.Server/CloudflareDdns.Server.csproj \
      -c $BUILD_CONFIGURATION \
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
      /p:PublishReadyToRun=true \
      -o /app/publish \
      --no-build

###############################################################################
# 3) Final: minimal Debian “Bookworm-slim” runtime-deps + non-root user
###############################################################################
FROM mcr.microsoft.com/dotnet/runtime-deps:8.0-bookworm-slim AS final

ARG APP_UID=1000
ARG APP_GID=1000

# create non-root user
RUN groupadd --gid $APP_GID app \
 && useradd --uid $APP_UID --gid $APP_GID \
            --home-dir /app --no-log-init --create-home app

WORKDIR /app
USER app

# copy the published single-file output
COPY --from=publish /app/publish ./

# tell Kestrel which ports to bind
ENV ASPNETCORE_URLS=http://+:8080;https://+:8081
EXPOSE 8080 8081

# run
ENTRYPOINT ["./CloudflareDdns.Server"]
