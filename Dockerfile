# Etapa 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copiamos todo el código fuente
COPY . .

# Restauramos y compilamos el proyecto principal
WORKDIR /src/src/MortalKombatCompiler.Backend/MortalKombatCompiler.API
RUN dotnet restore
RUN dotnet publish -c Release -o /app/out

# Etapa 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Copiamos el resultado del build
COPY --from=build /app/out .

# Railway expone por defecto el puerto 8080
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "MortalKombatCompiler.API.dll"]
