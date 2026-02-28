# ServiceTemplate

> Production-ready .NET 10 service template with Clean Architecture, Docker, Helm, and GitHub Actions CI/CD.

[![CI](https://github.com/your-org/service-template/actions/workflows/ci.yml/badge.svg)](https://github.com/your-org/service-template/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/your-org/service-template/branch/main/graph/badge.svg)](https://codecov.io/gh/your-org/service-template)

## Table of Contents

- [Architecture](#architecture)
- [Prerequisites](#prerequisites)
- [Quick Start](#quick-start)
- [Project Structure](#project-structure)
- [Development Workflow](#development-workflow)
- [Testing](#testing)
- [Docker](#docker)
- [Kubernetes / Helm](#kubernetes--helm)
- [CI/CD](#cicd)
- [Renaming the Template](#renaming-the-template)
- [Configuration Reference](#configuration-reference)

---

## Architecture

This template follows **Clean Architecture** (also known as Onion Architecture):

```
┌────────────────────────────────────────┐
│               Api (Entry Point)        │  Minimal APIs, middleware, DI wiring
├────────────────────────────────────────┤
│            Application Layer           │  CQRS (ISender/IRequestHandler), validators, DTOs
├────────────────────────────────────────┤
│              Domain Layer              │  Entities, value objects, domain errors
├────────────────────────────────────────┤
│           Infrastructure Layer         │  EF Core, PostgreSQL, repositories
└────────────────────────────────────────┘
```

**Key design decisions:**
- **Custom CQRS** — commands and queries dispatched via `ISender.SendAsync()` / `IRequestHandler.HandleAsync()`, with pipeline behaviors for logging and validation (MediatR is commercial — not used)
- **Result<T> pattern** — no exceptions for expected failure paths; errors flow as values
- **Testcontainers** — integration and acceptance tests spin up real PostgreSQL containers
- **OpenTelemetry** — traces, metrics, and logs exported via OTLP
- **Serilog** — structured logging with Seq support for local dev

---

## Prerequisites

| Tool | Minimum Version | Install |
|------|----------------|---------|
| .NET SDK | 10.0.x | [dotnet.microsoft.com](https://dotnet.microsoft.com/download) |
| Docker Desktop | 4.x | [docker.com](https://www.docker.com/products/docker-desktop) |
| `make` | any | `winget install GnuWin32.Make` (Windows) |
| `git` | 2.x | [git-scm.com](https://git-scm.com) |

**Optional (recommended):**
- VS Code with the [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit) extension
- [Dev Containers extension](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers) for a fully containerised dev environment

---

## Quick Start

### Option A — Native (recommended for speed)

```bash
# 1. Clone and enter the project
git clone https://github.com/your-org/service-template.git && cd service-template

# 2. One-command setup: installs tools, restores packages, starts infra, runs migrations
make setup

# 3. Start the API with hot reload
make run
```

The API is now available at `http://localhost:5000`.
- **Scalar API docs:** `http://localhost:5000/scalar`
- **Health check:** `http://localhost:5000/health`
- **Seq logs:** `http://localhost:5341`

### Option B — Dev Container (zero-install)

1. Open the repository in VS Code
2. When prompted, click **"Reopen in Container"**
3. Wait for the container to build (~2 min on first run)
4. Run `make setup` in the integrated terminal

---

## Project Structure

```
.
├── .devcontainer/              # VS Code Dev Container configuration
├── .github/
│   ├── workflows/
│   │   ├── ci.yml              # Build, test, Docker build on push/PR
│   │   ├── release.yml         # Push Docker image + Helm chart on tag
│   │   └── pr.yml              # PR title validation, format check
│   ├── CODEOWNERS
│   └── pull_request_template.md
├── deploy/
│   ├── helm/chart/             # Helm chart (deployment, HPA, PDB, ingress...)
│   └── otel/                   # OpenTelemetry Collector config
├── docs/adr/                   # Architecture Decision Records
├── src/
│   ├── Api/                    # ASP.NET Core Minimal API, Program.cs
│   ├── Application/            # Custom CQRS (ISender/IRequestHandler), validators, DTOs
│   ├── Domain/                 # Entities, domain errors, Result<T>
│   └── Infrastructure/         # EF Core, PostgreSQL, repositories
├── tests/
│   ├── UnitTests/              # Fast, isolated, no I/O
│   ├── IntegrationTests/       # Real DB via Testcontainers + Respawn
│   └── AcceptanceTests/        # BDD-style HTTP API tests
├── docker-compose.yml          # Full local stack
├── docker-compose.override.yml # Local overrides (auto-applied by Docker Compose)
├── Dockerfile                  # Multi-stage production build
├── Directory.Build.props       # Shared MSBuild settings for all projects
├── Directory.Packages.props    # Central NuGet package version management
├── global.json                 # Pins .NET SDK version
├── Makefile                    # Developer task runner
└── ServiceTemplate.sln
```

---

## Development Workflow

### Common Tasks

```bash
make help              # Show all available commands
make build             # Build the solution
make run               # Run API with hot reload
make fmt               # Auto-format code
make lint              # Verify formatting (CI-safe)
make clean             # Remove bin/obj/TestResults
make outdated          # List outdated NuGet packages
```

### Database Migrations

```bash
# Apply pending migrations
make migrate

# Create a new migration
make migration NAME=AddUserTable

# Roll back last migration
make migration-rollback
```

### Infrastructure (backing services only)

```bash
make infra-up    # Start Postgres + Seq + OTEL Collector
make infra-down  # Stop them
make infra-reset # Full reset — WARNING: deletes all data
```

---

## Testing

| Test Suite | Purpose | Command |
|-----------|---------|---------|
| Unit | Business logic in isolation (NSubstitute mocks) | `make test` |
| Integration | Full app + real PostgreSQL (Testcontainers) | `make test-integration` |
| Acceptance | BDD-style HTTP API tests | `make test-acceptance` |

```bash
make test-all      # Run every test suite
make coverage      # Unit tests + open HTML coverage report
```

**Coverage** is automatically uploaded to [Codecov](https://codecov.io) in CI.

---

## Docker

```bash
# Build the image
make docker-build

# Start the complete stack
make docker-up

# Tail logs
make docker-logs

# Stop everything
make docker-down
```

The multi-stage `Dockerfile` produces a minimal, non-root runtime image.

---

## Kubernetes / Helm

```bash
# Lint the chart
make helm-lint

# Render templates (dry-run)
make helm-template

# Package
make helm-package

# Deploy to a cluster (staging)
helm upgrade --install my-service deploy/helm/chart \
  -f deploy/helm/chart/values.staging.yaml \
  --set image.tag=1.2.3 \
  --namespace my-namespace --create-namespace

# Deploy to production
helm upgrade --install my-service deploy/helm/chart \
  -f deploy/helm/chart/values.production.yaml \
  --set image.tag=1.2.3 \
  --namespace production
```

**The chart includes:**
- Deployment with rolling updates and `maxUnavailable: 0`
- Horizontal Pod Autoscaler (CPU + memory)
- PodDisruptionBudget (`minAvailable: 1`)
- Non-root pod security context with `readOnlyRootFilesystem`
- Topology spread constraints for zone-level HA
- Liveness / readiness probes

---

## CI/CD

### Workflows

| Workflow | Trigger | What it does |
|---------|---------|-------------|
| `ci.yml` | Push to `main`/`develop`, PR | Build → Unit Tests → Integration Tests → Acceptance Tests → Docker build |
| `release.yml` | Push tag `v*.*.*` | All tests → Push Docker image to GHCR → Package & push Helm chart → GitHub Release |
| `pr.yml` | PR opened/updated | Validate PR title (Conventional Commits), check code formatting |

### Releasing

```bash
git tag v1.2.3
git push origin v1.2.3
```

This triggers the release pipeline which will:
1. Run all tests
2. Build and push `ghcr.io/your-org/service-template:1.2.3`
3. Push the Helm chart to `ghcr.io/your-org/helm-charts`
4. Create a GitHub Release with auto-generated notes

---

## Renaming the Template

To use this as a starting point for a new service named `PaymentService`:

```bash
# 1. Clone
git clone https://github.com/your-org/service-template.git payment-service
cd payment-service

# 2. Bulk rename (Linux/macOS)
find . -not -path './.git/*' \
  \( -name "*.cs" -o -name "*.csproj" -o -name "*.sln" -o -name "*.json" -o -name "*.yaml" -o -name "*.yml" \) \
  -exec sed -i 's/ServiceTemplate/PaymentService/g; s/service-template/payment-service/g' {} +

# 3. Rename files and directories
mv ServiceTemplate.sln PaymentService.sln
for d in src tests; do
  find $d -name "ServiceTemplate.*" | while read f; do
    mv "$f" "${f/ServiceTemplate/PaymentService}"
  done
done

# 4. Re-init git history
rm -rf .git && git init && git add . && git commit -m "chore: initial commit from service-template"
```

---

## Configuration Reference

| Key | Default | Description |
|-----|---------|-------------|
| `ConnectionStrings:DefaultConnection` | Postgres on localhost:5432 | PostgreSQL connection string |
| `OpenTelemetry:ServiceName` | `service-template` | Service name reported to OTEL |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | `http://localhost:4317` | OTLP gRPC endpoint |
| `Serilog:MinimumLevel:Default` | `Information` | Minimum log level |

Secrets (passwords, API keys) **must never be committed**. Use:
- Kubernetes Secrets (referenced in Helm chart)
- GitHub Actions secrets
- `.env` files locally (already in `.gitignore`)
