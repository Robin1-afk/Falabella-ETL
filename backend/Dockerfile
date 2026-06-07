# ── Etapa de compilación ──────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restaurar dependencias antes de copiar el código fuente (mejor aprovechamiento del caché)
COPY DataFlowPlatform.slnx .
COPY DataFlowPlatform.API/DataFlowPlatform.API.csproj             DataFlowPlatform.API/
COPY DataFlowPlatform.Domain/DataFlowPlatform.Domain.csproj       DataFlowPlatform.Domain/
COPY DataFlowPlatform.Infrastructure/DataFlowPlatform.Infrastructure.csproj DataFlowPlatform.Infrastructure/
RUN dotnet restore

COPY . .
RUN dotnet publish DataFlowPlatform.API/DataFlowPlatform.API.csproj \
    -c Release -o /app/publish --no-restore

# ── Etapa de ejecución ────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5228
ENV ASPNETCORE_URLS=http://+:5228
ENTRYPOINT ["dotnet", "DataFlowPlatform.API.dll"]
