# syntax=docker/dockerfile:1.7

FROM mcr.microsoft.com/dotnet/sdk:10.0.101-bookworm-slim AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props ./
COPY src/Domain/SponsorshipApproval.Domain.csproj src/Domain/
COPY src/Application/SponsorshipApproval.Application.csproj src/Application/
COPY src/Infrastructure/SponsorshipApproval.Infrastructure.csproj src/Infrastructure/
COPY src/Api/SponsorshipApproval.Api.csproj src/Api/
RUN dotnet restore src/Api/SponsorshipApproval.Api.csproj

COPY . .
RUN dotnet publish src/Api/SponsorshipApproval.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0.1-bookworm-slim AS runtime
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

FROM runtime AS migrator-placeholder
ENTRYPOINT ["sh", "-c", "echo 'No EF migrations exist yet; T1.1 will replace this placeholder.'"]
