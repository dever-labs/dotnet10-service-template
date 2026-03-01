#!/bin/sh
# Runs once after the dev container is created.
# Installs tools (kind, mirrord) and restores .NET packages.
set -eu

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

# ── mirrord ───────────────────────────────────────────────────────────────────
# mirrord gives the local process cluster network access (DNS, services)
# without deploying the app into k8s — run `make dev` to start with it.
if ! command -v mirrord &>/dev/null; then
  echo "→ Installing mirrord..."
  curl -fsSL https://raw.githubusercontent.com/metalbear-co/mirrord/main/scripts/install.sh | bash
else
  echo "→ mirrord already installed: $(mirrord --version)"
fi

# ── .NET restore ──────────────────────────────────────────────────────────────
# Try the org NuGet proxy first (nuget.config). Fall back to nuget.org when
# the proxy is not yet configured — this lets the template work out of the box
# before nuget.config is updated with the real org feed URL.
echo "→ Restoring .NET packages..."
if ! dotnet restore --ignore-failed-sources 2>/dev/null; then
  echo "⚠  nuget.internal unreachable — falling back to nuget.org"
  echo "   Update nuget.config with your org's NuGet proxy to remove this warning."
  dotnet restore --source "https://api.nuget.org/v3/index.json"
fi

echo "→ Restoring .NET tools (dotnet-ef, reportgenerator)..."
if ! dotnet tool restore 2>/dev/null; then
  dotnet tool restore --add-source "https://api.nuget.org/v3/index.json"
fi

# ── Helm repos ────────────────────────────────────────────────────────────────
echo "→ Adding Helm repositories..."
helm repo add bitnami https://charts.bitnami.com/bitnami 2>/dev/null || helm repo update bitnami
helm repo add open-telemetry https://open-telemetry.github.io/opentelemetry-helm-charts 2>/dev/null || helm repo update open-telemetry
helm repo update

echo "→ Updating Helm chart dependencies..."
helm dependency update deploy/helm

echo ""
echo "✅ Dev container ready!"
echo ""
echo "   Next steps:"
echo "     make cluster-create   — create local kind cluster + registry"
echo "     make dev-deps          — deploy fake near-dependencies into the cluster"
echo "     make dev               — run the API locally with cluster network access (mirrord)"
echo ""
