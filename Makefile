.DEFAULT_GOAL := help
SHELL := /bin/bash
.SHELLFLAGS := -eu -o pipefail -c

# ── Variables ──────────────────────────────────────────────────────────────────
PROJECT        := ServiceTemplate
SOLUTION       := ServiceTemplate.sln
SRC_API        := src/Api
DOCKER_IMAGE   := service-template
VERSION        ?= $(shell git describe --tags --always --dirty 2>/dev/null || echo "0.0.1-local")
COMPOSE_FILE   := docker-compose.yml

# Colors
CYAN  := \033[36m
RESET := \033[0m

# ── Help ───────────────────────────────────────────────────────────────────────
.PHONY: help
help: ## Show this help
	@echo ""
	@echo "  $(CYAN)$(PROJECT) Development Tasks$(RESET)"
	@echo ""
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) \
		| awk 'BEGIN {FS = ":.*?## "}; {printf "  $(CYAN)%-22s$(RESET) %s\n", $$1, $$2}'
	@echo ""

# ── Setup ──────────────────────────────────────────────────────────────────────
.PHONY: setup
setup: ## First-time project setup (installs tools, restores packages)
	@echo "→ Installing .NET tools..."
	dotnet tool restore
	@echo "→ Restoring NuGet packages..."
	dotnet restore
	@echo "→ Starting infrastructure (Postgres, Seq, OTEL)..."
	$(MAKE) infra-up
	@echo "→ Running database migrations..."
	$(MAKE) migrate
	@echo ""
	@echo "✅ Setup complete! Run 'make run' to start the API."

# ── Build ──────────────────────────────────────────────────────────────────────
.PHONY: build
build: ## Build the solution
	dotnet build $(SOLUTION) -c Release --no-restore -p:TreatWarningsAsErrors=true

.PHONY: build-dev
build-dev: ## Build in Debug mode
	dotnet build $(SOLUTION) -c Debug

# ── Run ────────────────────────────────────────────────────────────────────────
.PHONY: run
run: ## Run the API locally (hot reload)
	dotnet watch run --project $(SRC_API) --launch-profile Development

.PHONY: run-docker
run-docker: ## Run the full stack with Docker Compose
	docker compose -f $(COMPOSE_FILE) up --build

# ── Tests ──────────────────────────────────────────────────────────────────────
.PHONY: test
test: ## Run unit tests
	dotnet test tests/UnitTests \
		-c Release \
		--no-restore \
		--logger "console;verbosity=normal" \
		--collect:"XPlat Code Coverage"

.PHONY: test-integration
test-integration: ## Run integration tests (requires Docker)
	dotnet test tests/IntegrationTests \
		-c Release \
		--no-restore \
		--logger "console;verbosity=normal"

.PHONY: test-acceptance
test-acceptance: ## Run acceptance tests (requires Docker)
	dotnet test tests/AcceptanceTests \
		-c Release \
		--no-restore \
		--logger "console;verbosity=normal"

.PHONY: test-all
test-all: test test-integration test-acceptance ## Run all tests

.PHONY: coverage
coverage: ## Run unit tests and open HTML coverage report
	dotnet test tests/UnitTests \
		-c Release \
		--no-restore \
		--collect:"XPlat Code Coverage" \
		--results-directory TestResults
	dotnet tool run reportgenerator \
		-reports:"TestResults/**/coverage.cobertura.xml" \
		-targetdir:"TestResults/CoverageReport" \
		-reporttypes:Html
	@echo "→ Coverage report: TestResults/CoverageReport/index.html"

# ── Code Quality ───────────────────────────────────────────────────────────────
.PHONY: fmt
fmt: ## Format all code
	dotnet format $(SOLUTION)

.PHONY: lint
lint: ## Check formatting without modifying files
	dotnet format $(SOLUTION) --verify-no-changes --severity warn

# ── Database ───────────────────────────────────────────────────────────────────
.PHONY: migrate
migrate: ## Apply EF Core migrations
	dotnet ef database update --project src/Infrastructure --startup-project src/Api

.PHONY: migration
migration: ## Create a new migration: make migration NAME=AddNewTable
	@test -n "$(NAME)" || (echo "ERROR: NAME is required. Usage: make migration NAME=AddNewTable"; exit 1)
	dotnet ef migrations add $(NAME) --project src/Infrastructure --startup-project src/Api

.PHONY: migration-rollback
migration-rollback: ## Roll back to previous migration
	dotnet ef migrations remove --project src/Infrastructure --startup-project src/Api

# ── Infrastructure ─────────────────────────────────────────────────────────────
.PHONY: infra-up
infra-up: ## Start backing services (Postgres, Seq, OTEL Collector)
	docker compose -f $(COMPOSE_FILE) up -d postgres seq otel-collector

.PHONY: infra-down
infra-down: ## Stop backing services
	docker compose -f $(COMPOSE_FILE) down

.PHONY: infra-reset
infra-reset: ## Destroy and recreate all infrastructure (WARNING: deletes data)
	docker compose -f $(COMPOSE_FILE) down -v
	$(MAKE) infra-up
	sleep 3
	$(MAKE) migrate

# ── Docker ─────────────────────────────────────────────────────────────────────
.PHONY: docker-build
docker-build: ## Build the Docker image
	docker build -t $(DOCKER_IMAGE):$(VERSION) -t $(DOCKER_IMAGE):local --build-arg VERSION=$(VERSION) .

.PHONY: docker-up
docker-up: ## Start full stack with Docker Compose
	docker compose -f $(COMPOSE_FILE) up -d

.PHONY: docker-down
docker-down: ## Stop full stack
	docker compose -f $(COMPOSE_FILE) down

.PHONY: docker-logs
docker-logs: ## Tail Docker Compose logs
	docker compose -f $(COMPOSE_FILE) logs -f

# ── Helm ───────────────────────────────────────────────────────────────────────
.PHONY: helm-lint
helm-lint: ## Lint the Helm chart
	helm lint deploy/helm/chart

.PHONY: helm-template
helm-template: ## Render Helm templates (dry run)
	helm template service-template deploy/helm/chart --debug

.PHONY: helm-package
helm-package: ## Package the Helm chart
	helm package deploy/helm/chart --version $(VERSION) --app-version $(VERSION)

# ── Utilities ──────────────────────────────────────────────────────────────────
.PHONY: clean
clean: ## Remove build artifacts
	find . -type d -name bin -not -path "./.git/*" -exec rm -rf {} + 2>/dev/null || true
	find . -type d -name obj -not -path "./.git/*" -exec rm -rf {} + 2>/dev/null || true
	rm -rf TestResults/

.PHONY: restore
restore: ## Restore NuGet packages
	dotnet restore $(SOLUTION)

.PHONY: outdated
outdated: ## List outdated NuGet packages
	dotnet list package --outdated
