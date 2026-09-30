# syntax=docker/dockerfile:1

# Build on the machine's own architecture and cross-compile for the target (e.g. linux/amd64 from an
# Apple Silicon Mac), instead of emulating the whole SDK.
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
WORKDIR /src

# Restore first, from project files only, so the package layer is cached until a .csproj changes.
# The root build files must come too, or the projects lose their shared settings and versions.
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/Innovark.Weather.Api/*.csproj            src/Innovark.Weather.Api/
COPY src/Innovark.Weather.Application/*.csproj    src/Innovark.Weather.Application/
COPY src/Innovark.Weather.Infrastructure/*.csproj src/Innovark.Weather.Infrastructure/
RUN dotnet restore src/Innovark.Weather.Api/Innovark.Weather.Api.csproj -a $TARGETARCH

COPY src/ src/
RUN dotnet publish src/Innovark.Weather.Api/Innovark.Weather.Api.csproj \
    -c Release -a $TARGETARCH --no-restore \
    -o /app/publish /p:UseAppHost=false

# Chiseled: Ubuntu with only what .NET needs. No shell, no package manager, non-root by default.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# DOTNET_EnableDiagnostics=0 stops the runtime creating IPC files, so the root filesystem can be
# read-only (read_only in docker-compose).
ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_EnableDiagnostics=0
EXPOSE 8080
USER $APP_UID

# Exec form, so the app is PID 1 and receives SIGTERM directly for a graceful shutdown.
ENTRYPOINT ["dotnet", "Innovark.Weather.Api.dll"]
