FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY backend/F24/F24.csproj backend/F24/
RUN dotnet restore backend/F24/F24.csproj

COPY backend/F24/ backend/F24/
WORKDIR /src/backend/F24
RUN dotnet publish --configuration Release --output /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

HEALTHCHECK --interval=5s --timeout=5s --retries=10 CMD curl --fail http://localhost:5000/health || exit 1

ENTRYPOINT ["dotnet", "F24.dll"]
