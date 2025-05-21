# syntax=docker/dockerfile:1.4

###############################################################################
# 1) Build + restore
###############################################################################
FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim AS build  
ARG BUILD_CONFIGURATION=Release

WORKDIR /src

# Copy only the csproj files first for Docker layer caching
COPY _src/CloudflareDdns.Server/CloudflareDdns.Server.csproj \
     _src/Devv.CloudflareDdns/Devv.CloudflareDdns.csproj ./

RUN dotnet restore CloudflareDdns.Server.csproj

# Copy the rest of your code
COPY . .

# Build binaries
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

WORKDIR /src

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
# 3) Final: minimal Debian “Bookworm-slim” runtime-deps
###############################################################################
FROM mcr.microsoft.com/dotnet/runtime-deps:8.0-bookworm-slim AS final 

ARG APP_UID=1000
ARG APP_GID=1000

# Create a non-root app user
RUN groupadd --gid $APP_GID app \
 && useradd --uid $APP_UID --gid $APP_GID \
            --home-dir /app --no-log-init --create-home app

WORKDIR /app
USER app

# Copy the published single-file app
COPY --from=publish /app/publish ./

# Tell Kestrel which ports to listen on
ENV ASPNETCORE_URLS=http://+:8080;https://+:8081
EXPOSE 8080 8081

# Run it!
ENTRYPOINT ["./CloudflareDdns.Server"]
