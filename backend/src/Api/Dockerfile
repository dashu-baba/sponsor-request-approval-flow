# syntax=docker/dockerfile:1.7

FROM mcr.microsoft.com/dotnet/sdk:10.0.101-noble AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props ./
COPY .config/dotnet-tools.json .config/
COPY src/Domain/SponsorshipApproval.Domain.csproj src/Domain/
COPY src/Application/SponsorshipApproval.Application.csproj src/Application/
COPY src/Infrastructure/SponsorshipApproval.Infrastructure.csproj src/Infrastructure/
COPY src/Api/SponsorshipApproval.Api.csproj src/Api/
RUN dotnet restore src/Api/SponsorshipApproval.Api.csproj
RUN dotnet tool restore

COPY . .
RUN dotnet publish src/Api/SponsorshipApproval.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0.1-noble AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && addgroup --system --gid 10001 appgroup \
    && adduser --system --uid 10001 --ingroup appgroup appuser

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

USER appuser

ENTRYPOINT ["dotnet", "SponsorshipApproval.Api.dll"]

FROM build AS migrator
ENTRYPOINT ["dotnet", "tool", "run", "dotnet-ef", "database", "update", "--project", "src/Infrastructure/SponsorshipApproval.Infrastructure.csproj", "--startup-project", "src/Api/SponsorshipApproval.Api.csproj", "--context", "AppDbContext", "--configuration", "Release", "--no-build"]
