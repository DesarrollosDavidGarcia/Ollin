# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY TlatoaniShared/TlatoaniShared.csproj TlatoaniShared/
COPY Ollin.csproj .
RUN dotnet restore Ollin.csproj

COPY TlatoaniShared/ TlatoaniShared/
COPY . .
RUN dotnet publish Ollin.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8081
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8081

ENTRYPOINT ["dotnet", "Ollin.dll"]
