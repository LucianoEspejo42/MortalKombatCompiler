# Etapa 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0-bookworm-slim AS build
WORKDIR /src

COPY . .

WORKDIR /src/src/MortalKombatCompiler.Backend/MortalKombatCompiler.API
RUN dotnet --version
RUN dotnet restore
RUN dotnet publish -c Release -o /app/out

# Etapa 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0-bookworm-slim AS runtime
WORKDIR /app
COPY --from=build /app/out .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "MortalKombatCompiler.API.dll"]
