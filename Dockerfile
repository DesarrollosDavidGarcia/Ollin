# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files for restore
COPY Ollin/TlatoaniShared/TlatoaniShared.csproj Ollin/TlatoaniShared/
COPY Ollin/Ollin.csproj Ollin/
RUN dotnet restore Ollin/Ollin.csproj

# Copy source and publish
COPY Ollin/ Ollin/
RUN dotnet publish Ollin/Ollin.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

RUN mkdir -p /app/data

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8081
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8081

ENTRYPOINT ["dotnet", "Ollin.dll"]
