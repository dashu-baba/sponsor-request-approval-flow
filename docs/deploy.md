# Deployment Runbook

This is the initial Docker Compose walking skeleton. Production TLS, hardening, backups, and real
migrations are added in later tasks.

## Prerequisites

- Docker Engine with the Compose plugin.
- Ports `80` available on the host.
- For local development, no external PostgreSQL or MinIO installation is required.

## Environment Setup

The compose file defaults to `.env.example` so the skeleton can start with:

```bash
docker compose up --build
```

For local overrides, copy the example values and point Compose at the local file:

```bash
cp .env.example .env
docker compose --env-file .env up --build
```

Keep `.env` out of git. Replace every `change-me-*` value before using this outside local
development. The `COMPOSE_ENV_FILE` value in `.env.example` tells Compose which service env file to
mount; the default compose command still falls back to `.env.example` when no local `.env` exists.

## Services

- `nginx` publishes host port `80`, serves the built SPA, and proxies `/api`, `/health`,
  `/openapi`, and `/scalar` to the API container.
- `api` runs the ASP.NET Core service on the internal network.
- `db` runs PostgreSQL 17 with a named volume.
- `minio` runs S3-compatible object storage with a named volume.
- `migrator` is a one-shot placeholder. T1.1 replaces it with EF Core migration execution.

The `app-internal` network is marked `internal: true`, so services can talk to each other but do not
have outbound internet access from that network. Keep that restriction unless a later task adds a
runtime dependency that legitimately needs egress; in that case, attach only that service to a
separate external network.

## Smoke Checks

Start the stack:

```bash
docker compose up --build
```

In another terminal:

```bash
curl --fail http://localhost/
curl --fail http://localhost/api/health
curl --fail http://localhost/health
```

The health checks should return success once `db`, `migrator`, `api`, and `nginx` are healthy.

## Shutdown

Stop containers while keeping data volumes:

```bash
docker compose down
```

Remove local data volumes when you want a clean reset:

```bash
docker compose down --volumes
```
