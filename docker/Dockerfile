# ================================================================
# OmniRetail AI — Dockerfile Multi-stage Enterprise
#
# Stages :
#   base    — Runtime .NET 8
#   build   — Build + Restore + Publish
#   final   — Image finale légère (non-root)
# ================================================================

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
WORKDIR /app
EXPOSE 8080

# Créer un utilisateur non-root (sécurité)
RUN addgroup -S omniretail && adduser -S omniretail -G omniretail

# ── Build ──────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copier les fichiers projet d'abord (meilleur cache Docker)
COPY ["OmniRetail.API/OmniRetail.API.csproj",             "OmniRetail.API/"]
COPY ["OmniRetail.Application/OmniRetail.Application.csproj", "OmniRetail.Application/"]
COPY ["OmniRetail.Core/OmniRetail.Core.csproj",           "OmniRetail.Core/"]
COPY ["OmniRetail.Infrastructure/OmniRetail.Infrastructure.csproj", "OmniRetail.Infrastructure/"]

# Restore
RUN dotnet restore "OmniRetail.API/OmniRetail.API.csproj" \
    --runtime linux-musl-x64

# Copier le reste du code source
COPY . .

# Build Release
RUN dotnet build "OmniRetail.API/OmniRetail.API.csproj" \
    -c Release \
    -o /app/build \
    --no-restore

# ── Publish ────────────────────────────────────────────────────
FROM build AS publish
RUN dotnet publish "OmniRetail.API/OmniRetail.API.csproj" \
    -c Release \
    -o /app/publish \
    --runtime linux-musl-x64 \
    --self-contained false \
    --no-restore \
    /p:UseAppHost=false

# ── Final image ────────────────────────────────────────────────
FROM base AS final
WORKDIR /app

# Créer le dossier Logs avec les bonnes permissions
RUN mkdir -p /app/Logs && chown omniretail:omniretail /app/Logs

# Copier l'application publiée
COPY --from=publish /app/publish .

# Changer de propriétaire
RUN chown -R omniretail:omniretail /app

# Exécuter en non-root
USER omniretail

# Variables d'environnement par défaut
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

ENTRYPOINT ["dotnet", "OmniRetail.API.dll"]
