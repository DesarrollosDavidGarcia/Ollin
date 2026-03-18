# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy shared project first
COPY TlatoaniShared/TlatoaniShared.csproj TlatoaniShared/
COPY Ollin/Ollin.csproj Ollin/
RUN dotnet restore Ollin/Ollin.csproj

COPY TlatoaniShared/ TlatoaniShared/
COPY Ollin/ Ollin/
RUN dotnet publish Ollin/Ollin.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8081
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8081

ENTRYPOINT ["dotnet", "Ollin.dll"]
