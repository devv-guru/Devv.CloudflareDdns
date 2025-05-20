# 1) Restore + build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY CloudflareDdns.Server/CloudflareDdns.Server.csproj ./CloudflareDdns.Server/
RUN dotnet restore ./CloudflareDdns.Server/CloudflareDdns.Server.csproj
COPY CloudflareDdns.Server/. ./CloudflareDdns.Server/
WORKDIR /src/CloudflareDdns.Server
RUN dotnet build -c $BUILD_CONFIGURATION -o /app/build

# 2) Publish trimmed, single-file
FROM build AS publish
ARG BUILD_CONFIGURATION=Release

RUN dotnet publish \
    ./CloudflareDdns.Server.csproj \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    -r linux-x64 \
    /p:SelfContained=true \
    /p:PublishSingleFile=true \
    /p:PublishTrimmed=true \
    /p:PublishReadyToRun=true \
    /p:UseAppHost=true

# 3) Final: Debian 12 slim runtime-deps
FROM mcr.microsoft.com/dotnet/runtime-deps:8.0-bookworm-slim AS final
USER app
WORKDIR /app
COPY --from=publish /app/publish ./
ENTRYPOINT ["./CloudflareDdns.Server"]
EXPOSE 8080
