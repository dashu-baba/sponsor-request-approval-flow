# Production Deployment Guide

## Overview

This document covers the production infrastructure, deployment pipeline, security model, and operational runbook for the Sponsorship Request Approval Flow application.

---

## Architecture

### Infrastructure Stack

| Layer | Technology |
|-------|-----------|
| DNS | Cloudflare (proxied, Flexible SSL) |
| Compute | AWS EC2 — Ubuntu 24.04, single instance |
| Container runtime | Docker + Docker Compose |
| Object storage | MinIO (S3-compatible, containerised) |
| Database | PostgreSQL 17 (containerised) |
| Remote management | AWS Systems Manager (SSM) Session Manager |
| CI/CD | GitHub Actions + OIDC |
| Image registry | GitHub Container Registry (GHCR) |

### Network Topology

```
User Browser
     │  HTTPS (443)
     ▼
Cloudflare Edge  ──── SSL termination (Flexible mode)
     │  HTTP (80)
     ▼
EC2 Instance (Elastic IP: 34.200.61.237)
     │
     ▼
┌─────────────────────────────────────────────┐
│  Docker network: edge                        │
│  ┌─────────────────────────────────────┐    │
│  │  nginx (frontend) :80 → :8080       │    │
│  └──────────────┬──────────────────────┘    │
│                 │  Docker network: app-internal (isolated)
│  ┌──────────────▼──────────────────────┐    │
│  │  api (:8080)                        │    │
│  │  db  (PostgreSQL :5432)             │    │
│  │  minio (:9000)                      │    │
│  └─────────────────────────────────────┘    │
└─────────────────────────────────────────────┘
```

### CI/CD Pipeline

```
Push to main
     │
     ├──────────────────────┐
     ▼                      ▼
ci-backend              ci-frontend
(build, format,         (typecheck, lint,
 test, dotnet test)      format, build, test)
     │                      │
     └──────────┬───────────┘
                │  both must pass
                ▼
         build-and-push
         ┌─────────────────────────────┐
         │  Build API image  (:runtime)│
         │  Build migrator   (:migrator│
         │  Build frontend             │
         │  Push all → GHCR            │
         └──────────────┬──────────────┘
                        │
                        ▼
                     deploy
                        │
                        │  OIDC token (no stored credentials)
                        ▼
                   AWS STS → IAM Role (github-actions-deploy)
                        │
                        │  ssm:SendCommand
                        ▼
                   SSM Agent on EC2
                        │
                        ▼
              docker compose pull
              docker compose up -d
              docker image prune -f
```

### Authentication Flow (OIDC — no stored AWS credentials)

```
GitHub Actions runner
     │
     │  1. Request OIDC token from GitHub
     ▼
GitHub OIDC Provider (token.actions.githubusercontent.com)
     │
     │  2. Present token to AWS STS
     ▼
AWS STS AssumeRoleWithWebIdentity
     │
     │  3. Return temporary credentials (15 min TTL)
     ▼
IAM Role: github-actions-deploy
     │
     │  4. Use credentials to call ssm:SendCommand
     ▼
EC2 Instance (via SSM — no SSH, no port 22)
```

---

## AWS Resources

### IAM Roles

#### `ec2-sponsorship-app` (EC2 Instance Profile)
- **Purpose:** Allows SSM Agent to register and receive commands
- **Attached policy:** `AmazonSSMManagedInstanceCore` (AWS managed)
- **Extra inline policy:** `ssm-parameter-read` — allows reading `/sponsorship-app/*` from Parameter Store

#### `github-actions-deploy` (GitHub Actions)
- **Purpose:** Assumed by GitHub Actions via OIDC — no long-lived keys
- **Trust condition:** Scoped to `repo:dashu-baba/sponsor-request-approval-flow:ref:refs/heads/main` only
- **Inline policy:** `ssm-deploy-policy`
  - `ssm:SendCommand` on the specific EC2 instance and `AWS-RunShellScript` document
  - `ssm:GetCommandInvocation` to poll result

### SSM Parameter Store

| Parameter | Type | Purpose |
|-----------|------|---------|
| `/sponsorship-app/ghcr-token` | SecureString | Read-only GitHub PAT for pulling images from GHCR |

### Security Group

| Direction | Port | Source | Reason |
|-----------|------|--------|--------|
| Inbound | 80 | 0.0.0.0/0 | HTTP (Cloudflare proxies to this) |
| Inbound | 22 | ❌ closed | SSH not needed — use SSM Session Manager |
| Outbound | 443 | 0.0.0.0/0 | SSM Agent, GHCR, AWS APIs |

---

## Environment Configuration

The `.env` file lives at `/opt/sponsorship-app/.env` on EC2. It is never committed to the repository.

```env
ASPNETCORE_ENVIRONMENT=Production

POSTGRES_DB=sponsorship_approval
POSTGRES_USER=sponsorship_app
POSTGRES_PASSWORD=<strong-password>

MINIO_ROOT_USER=<minio-user>
MINIO_ROOT_PASSWORD=<strong-password>

ConnectionStrings__Default=Host=db;Port=5432;Database=sponsorship_approval;Username=sponsorship_app;Password=<same-as-POSTGRES_PASSWORD>

Minio__Endpoint=http://minio:9000
Minio__AccessKey=<same-as-MINIO_ROOT_USER>
Minio__SecretKey=<same-as-MINIO_ROOT_PASSWORD>
Minio__BucketName=sponsorship-attachments

Jwt__Issuer=sponsorship-approval
Jwt__Audience=sponsorship-approval-api
Jwt__SigningKey=<random-string-min-32-chars>
Jwt__AccessTokenLifetimeMinutes=15
Jwt__RefreshTokenLifetimeDays=7

# Bootstrap: creates the first SystemAdmin on first boot if no users exist.
# Safe to remove after first login.
Bootstrap__AdminEmail=admin@yourdomain.com
Bootstrap__AdminPassword=<strong-password>
Bootstrap__AdminDisplayName=Admin

# Do NOT set SEED_DEMO_DATA=true in production.
```

### Seeding Behaviour

| Variable | Value | Result |
|----------|-------|--------|
| `SEED_DEMO_DATA` | `true` | Seeds demo users, sponsorship types, and sample requests (dev/test only) |
| `SEED_DEMO_DATA` | absent or `false` | Roles always seeded; bootstrap admin created on first boot if no users exist |

---

## Deployment Pipeline

### Trigger
Every push to `main` triggers the full pipeline automatically.

### Jobs (in order)

```
ci-backend ──┐
             ├──► build-and-push ──► deploy
ci-frontend ─┘
```

1. **ci-backend** — restore, build (warnings as errors), format check, dotnet test
2. **ci-frontend** — install, typecheck, lint, format check, build, vitest
3. **build-and-push** — builds three Docker images and pushes to GHCR:
   - `ghcr.io/dashu-baba/sponsor-request-approval-flow/api:latest`
   - `ghcr.io/dashu-baba/sponsor-request-approval-flow/migrator:latest`
   - `ghcr.io/dashu-baba/sponsor-request-approval-flow/frontend:latest`
4. **deploy** — assumes IAM role via OIDC, sends SSM command to EC2 to pull and restart containers

### GitHub Actions Variables

| Variable | Description |
|----------|-------------|
| `AWS_DEPLOY_ROLE_ARN` | ARN of `github-actions-deploy` IAM role |
| `AWS_REGION` | AWS region e.g. `us-east-1` |
| `EC2_INSTANCE_ID` | EC2 instance ID e.g. `i-0abc…` |

No AWS credentials are stored as secrets — authentication is entirely via OIDC.

---

## Accessing the Server

Port 22 is closed. Use **SSM Session Manager** instead.

### Via AWS Console
1. Go to **AWS Systems Manager → Session Manager**
2. Click **Start session**
3. Select your instance → **Connect**

### Via AWS CLI
```bash
aws ssm start-session --target i-YOUR_INSTANCE_ID --region YOUR_REGION
```

---

## Operational Runbook

### Check container status
```bash
cd /opt/sponsorship-app
docker compose ps
```

### View logs
```bash
docker compose logs api --tail=50
docker compose logs nginx --tail=50
docker compose logs db --tail=50
```

### Restart all containers
```bash
cd /opt/sponsorship-app
docker compose restart
```

### Full redeploy manually
```bash
cd /opt/sponsorship-app
TOKEN=$(aws ssm get-parameter --name /sponsorship-app/ghcr-token \
  --with-decryption --query Parameter.Value --output text --region YOUR_REGION)
echo "$TOKEN" | docker login ghcr.io -u dashu-baba --password-stdin
docker compose pull
docker compose up -d --remove-orphans
docker image prune -f
```

### Free up disk space
```bash
docker image prune -af
docker volume prune -f   # WARNING: removes unused volumes including db-data if stopped
```

### Check disk usage
```bash
df -h /
docker system df
```

---

## Troubleshooting

| Symptom | Likely cause | Fix |
|---------|-------------|-----|
| "Web server is down" in browser | Container not running or Cloudflare SSL mode wrong | `docker compose ps` to check; set Cloudflare SSL to Flexible |
| API container restarting | Wrong image built (migrator entrypoint) or missing `.env` | Check `docker logs sponsorship-app-api-1`; verify `.env` exists |
| SSM command stays Pending | SSM Agent not running or EC2 role not attached | `sudo systemctl status snap.amazon-ssm-agent.amazon-ssm-agent`; check Fleet Manager |
| Disk full during deploy | EC2 volume too small | Expand EBS volume in console, then `sudo growpart /dev/nvme0n1 1 && sudo resize2fs /dev/nvme0n1p1` |
| Bootstrap admin not created | `Bootstrap__AdminEmail` missing from `.env` or users already exist | Check `.env`; if users exist, create via admin UI |
| Images fail to pull | GHCR token expired | Regenerate PAT on GitHub, update SSM Parameter Store |

---

## Best Practices

- **No SSH keys** — access via SSM Session Manager only; port 22 stays closed
- **No long-lived AWS credentials** — GitHub Actions authenticates via OIDC; credentials last 15 minutes
- **No secrets in the repo** — `.env` lives only on EC2; GHCR token stored in SSM Parameter Store (SecureString)
- **Least privilege** — `github-actions-deploy` can only send SSM commands to this specific instance; nothing else
- **CI gates deploy** — both backend and frontend CI must pass before any image is built or deployed
- **Idempotent migrations** — EF Core migrator runs on every deploy; safe to run multiple times
- **Demo data off in production** — `SEED_DEMO_DATA` is not set; only roles and bootstrap admin are created
- **Separate Docker networks** — only nginx is on the `edge` network; db, minio, and api are internal-only
