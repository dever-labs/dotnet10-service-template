.DEFAULT_GOAL := help
SHELL := /bin/bash
.SHELLFLAGS := -eu -o pipefail -c

# ── Variables ──────────────────────────────────────────────────────────────────
PROJECT        := ServiceTemplate
SOLUTION       := ServiceTemplate.sln
SRC_API        := src/Api
VERSION        ?= $(shell git describe --tags --always --dirty 2>/dev/null || echo "0.0.1-local")
CLUSTER_NAME   := service-template
REGISTRY       := localhost:5001
IMAGE          := $(REGISTRY)/service-template
HELM_CHART     := deploy/helm
HELM_FAKE      := deploy/fake
HELM_RELEASE   := service-template
NAMESPACE      := default

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
		| awk 'BEGIN {FS = ":.*?## "}; {printf "  $(CYAN)%-24s$(RESET) %s\n", $$1, $$2}'
	@echo ""

# ── Setup ──────────────────────────────────────────────────────────────────────
.PHONY: setup
setup: ## First-time setup: install tools, restore packages, add Helm repos
	@echo "→ Restoring .NET tools and packages..."
	dotnet tool restore
	dotnet restore
	@echo "→ Adding Helm repositories..."
	helm repo add bitnami https://charts.bitnami.com/bitnami 2>/dev/null || true
	helm repo add open-telemetry https://open-telemetry.github.io/opentelemetry-helm-charts 2>/dev/null || true
	helm repo update
	$(MAKE) helm-deps
	@echo ""
	@echo "✅ Setup complete! Next: make cluster-create && make dev"

# ── Cluster ────────────────────────────────────────────────────────────────────
.PHONY: cluster-create
cluster-create: ## Create local kind cluster with local Docker registry
	bash scripts/kind-with-registry.sh $(CLUSTER_NAME)

.PHONY: cluster-delete
cluster-delete: ## Delete local kind cluster and registry
	@echo "→ Deleting cluster '$(CLUSTER_NAME)'..."
	kind delete cluster --name $(CLUSTER_NAME)
	@echo "→ Stopping registry..."
	docker rm -f kind-registry 2>/dev/null || true

.PHONY: cluster-status
cluster-status: ## Show cluster nodes, registry, and Helm release status
	@echo "── Nodes ────────────────────────────────────────────"
	@kubectl get nodes -o wide 2>/dev/null || echo "(no cluster)"
	@echo ""
	@echo "── Registry ($(REGISTRY)) ──────────────────────────"
	@docker inspect -f 'Running: {{.State.Running}}' kind-registry 2>/dev/null || echo "(not running)"
	@echo ""
	@echo "── Helm releases ────────────────────────────────────"
	@helm list -n $(NAMESPACE) 2>/dev/null || true

# ── Dev loop ───────────────────────────────────────────────────────────────────
.PHONY: dev-deps
dev-deps: ## Deploy fake near-dependencies into local kind cluster
	helm dependency update $(HELM_FAKE)
	helm upgrade --install dev-deps $(HELM_FAKE) \
		-f $(HELM_FAKE)/values.yaml \
		--namespace $(NAMESPACE) \
		--wait --timeout 5m

.PHONY: dev-deps-delete
dev-deps-delete: ## Remove fake near-dependencies from local cluster
	helm uninstall dev-deps --namespace $(NAMESPACE) 2>/dev/null || true

.PHONY: dev
dev: ## Run the API locally with cluster network access via mirrord (requires: make cluster-create && make dev-deps)
	mirrord exec --config .mirrord/mirrord.json -- \
		dotnet watch run --project $(SRC_API) --launch-profile Development

.PHONY: dev-run
dev-run: ## One-shot run without file watching
	mirrord exec --config .mirrord/mirrord.json -- \
		dotnet run --project $(SRC_API) --launch-profile Development

# ── Build ──────────────────────────────────────────────────────────────────────
.PHONY: build
build: ## Build the solution (Release, warnings as errors)
	dotnet build $(SOLUTION) -c Release --no-restore -p:TreatWarningsAsErrors=true

.PHONY: build-dev
build-dev: ## Build in Debug mode
	dotnet build $(SOLUTION) -c Debug

.PHONY: docker-build
docker-build: ## Build Docker image and push to local registry
	docker build -t $(IMAGE):$(VERSION) -t $(IMAGE):local --build-arg VERSION=$(VERSION) .
	docker push $(IMAGE):$(VERSION)
	docker push $(IMAGE):local

# ── Run (dotnet watch — no cluster needed) ────────────────────────────────────
.PHONY: run
run: ## Run the API with dotnet watch (needs local postgres port-forward or override)
	dotnet watch run --project $(SRC_API) --launch-profile Development

# ── Tests ──────────────────────────────────────────────────────────────────────
.PHONY: test
test: ## Run unit tests
	dotnet test tests/UnitTests \
		-c Release \
		--no-restore \
		--logger "console;verbosity=normal" \
		--collect:"XPlat Code Coverage"

.PHONY: test-integration
test-integration: ## Run integration tests (uses Testcontainers — no cluster needed)
	dotnet test tests/IntegrationTests \
		-c Release \
		--no-restore \
		--logger "console;verbosity=normal"

.PHONY: test-acceptance
test-acceptance: ## Run acceptance tests (uses Testcontainers — no cluster needed)
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

# ── Code quality ───────────────────────────────────────────────────────────────
.PHONY: fmt
fmt: ## Format all code
	dotnet format $(SOLUTION)

.PHONY: lint
lint: ## Check formatting without modifying files
	dotnet format $(SOLUTION) --verify-no-changes --severity warn

# ── Database ───────────────────────────────────────────────────────────────────
.PHONY: migrate
migrate: ## Apply EF Core migrations (port-forwards local cluster postgres)
	bash scripts/migrate-local.sh

.PHONY: migration
migration: ## Create a new EF Core migration: make migration NAME=AddNewTable
	@test -n "$(NAME)" || (echo "ERROR: NAME is required. Usage: make migration NAME=AddNewTable"; exit 1)
	dotnet ef migrations add $(NAME) --project src/Infrastructure --startup-project src/Api

.PHONY: migration-rollback
migration-rollback: ## Roll back to previous migration
	dotnet ef migrations remove --project src/Infrastructure --startup-project src/Api

# ── Helm ───────────────────────────────────────────────────────────────────────
.PHONY: helm-deps
helm-deps: ## Download/update Helm chart dependencies (subcharts)
	helm dependency update $(HELM_CHART)

.PHONY: helm-lint
helm-lint: ## Lint the Helm chart
	helm lint $(HELM_CHART) -f values.local.yaml

.PHONY: helm-template
helm-template: ## Render Helm templates with local values (dry run)
	helm template $(HELM_RELEASE) $(HELM_CHART) \
		-f $(HELM_CHART)/values.yaml \
		-f values.local.yaml \
		--debug

.PHONY: helm-install
helm-install: ## Install chart to local cluster
	helm upgrade --install $(HELM_RELEASE) $(HELM_CHART) \
		-f $(HELM_CHART)/values.yaml \
		-f values.local.yaml \
		--namespace $(NAMESPACE) \
		--wait --timeout 5m \
		--set image.tag=$(VERSION)

.PHONY: helm-uninstall
helm-uninstall: ## Uninstall chart from local cluster
	helm uninstall $(HELM_RELEASE) --namespace $(NAMESPACE)

.PHONY: helm-package
helm-package: ## Package the Helm chart for release
	helm package $(HELM_CHART) --version $(VERSION) --app-version $(VERSION)

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

