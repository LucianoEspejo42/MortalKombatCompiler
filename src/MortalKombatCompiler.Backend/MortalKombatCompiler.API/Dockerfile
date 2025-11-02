FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

# Copiar proyecto y restaurar dependencias
COPY ["MortalKombatCompiler.API.csproj", "."]
RUN dotnet restore "MortalKombatCompiler.API.csproj"

# Copiar todo el código y construir
COPY . .
RUN dotnet build "MortalKombatCompiler.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MortalKombatCompiler.API.csproj" -c Release -o /app/publish

# Imagen final
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENTRYPOINT ["dotnet", "MortalKombatCompiler.API.dll"]