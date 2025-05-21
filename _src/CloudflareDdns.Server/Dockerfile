# syntax=docker/dockerfile:1.4

###############################################################################
# 1) Build + restore
###############################################################################
FROM mcr.microsoft.com/dotnet/sdk:8.0-slim AS build
ARG BUILD_CONFIGURATION=Release

WORKDIR /src

# Copy only the csproj files first (for a faster restore)
COPY _src/CloudflareDdns.Server/CloudflareDdns.Server.csproj \
     _src/Devv.CloudflareDdns/Devv.CloudflareDdns.csproj ./

RUN dotnet restore ./CloudflareDdns.Server.csproj

# Copy the rest of your code
COPY . .

WORKDIR /src

# Build out the binaries
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
# 3) Final: minimal Debian “runtime-deps” with non-root user
###############################################################################
FROM mcr.microsoft.com/dotnet/runtime-deps:8.0-bookworm-slim AS final

# Create and switch to a non-root user
ARG APP_UID=1000
ARG APP_GID=1000
RUN groupadd --gid $APP_GID app \
 && useradd --uid $APP_UID --gid $APP_GID \
            --home-dir /app --no-log-init --create-home app

WORKDIR /app

# Copy over the single-file publish output
COPY --from=publish /app/publish ./

# Expose the same ports you’ve configured in ASP.NET Core
ENV ASPNETCORE_URLS=http://+:8080;https://+:8081
EXPOSE 8080 8081

USER app

# Launch the single-file Linux executable
ENTRYPOINT ["./CloudflareDdns.Server"]
