# Bygger og kjører Kriseportal.Web i Docker.
# Bygges fra rotmappen (se docker-compose.yml), fordi webprosjektet
# også trenger prosjektet Kriseportal.ServiceDefaults.

# Steg 1: bygger appen med .NET SDK (stort bilde med kompilator)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Kopierer bare prosjektfilene først. Da husker Docker NuGet-pakkene
# mellom bygg, og det går raskere når bare koden er endret.
COPY Kriseportal.Web/Kriseportal.Web.csproj Kriseportal.Web/
COPY Kriseportal.ServiceDefaults/Kriseportal.ServiceDefaults.csproj Kriseportal.ServiceDefaults/
RUN dotnet restore Kriseportal.Web/Kriseportal.Web.csproj

# Kopierer resten av koden og publiserer en ferdig versjon
COPY Kriseportal.Web/ Kriseportal.Web/
COPY Kriseportal.ServiceDefaults/ Kriseportal.ServiceDefaults/
RUN dotnet publish Kriseportal.Web/Kriseportal.Web.csproj -c Release -o /app/publish --no-restore

# Steg 2: kjører appen i et lite bilde som bare har det som trengs for å kjøre
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Sikkerhet: appen kjører som en vanlig bruker, ikke som root
USER $APP_UID

# Appen lytter på port 8080 inne i containeren
EXPOSE 8080
ENTRYPOINT ["dotnet", "Kriseportal.Web.dll"]