#!/usr/bin/env bash
# Runs once after the dev container is created.
# Installs tools not available as devcontainer features (kind, skaffold)
# and restores .NET packages.
set -euo pipefail

echo ""
echo "──────────────────────────────────────────"
echo "  ServiceTemplate dev container setup"
echo "──────────────────────────────────────────"

# ── kind ─────────────────────────────────────────────────────────────────────
KIND_VERSION="v0.25.0"
if ! command -v kind &>/dev/null; then
  echo "→ Installing kind ${KIND_VERSION}..."
  curl -sSLo /tmp/kind "https://kind.sigs.k8s.io/dl/${KIND_VERSION}/kind-linux-amd64"
  chmod +x /tmp/kind
  sudo mv /tmp/kind /usr/local/bin/kind
else
  echo "→ kind already installed: $(kind version)"
fi

# ── skaffold ─────────────────────────────────────────────────────────────────
if ! command -v skaffold &>/dev/null; then
  echo "→ Installing skaffold (latest stable)..."
  curl -sSLo /tmp/skaffold "https://storage.googleapis.com/skaffold/releases/latest/skaffold-linux-amd64"
  chmod +x /tmp/skaffold
  sudo mv /tmp/skaffold /usr/local/bin/skaffold
else
  echo "→ skaffold already installed: $(skaffold version)"
fi

# ── .NET ─────────────────────────────────────────────────────────────────────
echo "→ Restoring .NET packages..."
dotnet restore

echo "→ Restoring .NET tools (dotnet-ef, reportgenerator)..."
dotnet tool restore

# ── Helm repos ────────────────────────────────────────────────────────────────
echo "→ Adding Helm repositories..."
helm repo add bitnami https://charts.bitnami.com/bitnami 2>/dev/null || helm repo update bitnami
helm repo add open-telemetry https://open-telemetry.github.io/opentelemetry-helm-charts 2>/dev/null || helm repo update open-telemetry
helm repo update

echo "→ Updating Helm chart dependencies..."
helm dependency update deploy/helm/chart

echo ""
echo "✅ Dev container ready!"
echo ""
echo "   Next steps:"
echo "     make cluster-create   — create local kind cluster + registry"
echo "     make dev               — build, deploy & watch (skaffold dev)"
echo ""
