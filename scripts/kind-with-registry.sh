#!/usr/bin/env bash
# Sets up a local kind cluster with a co-located Docker registry.
# Based on the official kind + local registry guide:
#   https://kind.sigs.k8s.io/docs/user/local-registry/
#
# After this script:
#   - Registry: localhost:5001  (push images here)
#   - Cluster:  service-template (kubectl context = kind-service-template)
set -euo pipefail

CLUSTER_NAME="${1:-service-template}"
REGISTRY_NAME="kind-registry"
REGISTRY_PORT="5001"

# ── 1. Start local registry (idempotent) ─────────────────────────────────────
if [ "$(docker inspect -f '{{.State.Running}}' "${REGISTRY_NAME}" 2>/dev/null)" != "true" ]; then
  echo "→ Starting local registry on localhost:${REGISTRY_PORT}..."
  docker run -d \
    --restart=always \
    -p "127.0.0.1:${REGISTRY_PORT}:5000" \
    --name "${REGISTRY_NAME}" \
    registry:2
else
  echo "→ Registry already running."
fi

# ── 2. Create kind cluster (idempotent) ──────────────────────────────────────
if kind get clusters 2>/dev/null | grep -q "^${CLUSTER_NAME}$"; then
  echo "→ Cluster '${CLUSTER_NAME}' already exists."
else
  echo "→ Creating kind cluster '${CLUSTER_NAME}'..."
  kind create cluster \
    --name "${CLUSTER_NAME}" \
    --config .kind/cluster.yaml
fi

# ── 3. Connect registry to kind network (idempotent) ─────────────────────────
if ! docker network inspect kind 2>/dev/null | grep -q "${REGISTRY_NAME}"; then
  echo "→ Connecting registry to kind network..."
  docker network connect kind "${REGISTRY_NAME}" 2>/dev/null || true
fi

# ── 4. Annotate nodes so tools can discover the registry ─────────────────────
echo "→ Annotating nodes with registry info..."
for node in $(kind get nodes --name "${CLUSTER_NAME}"); do
  kubectl annotate node "${node}" \
    --overwrite \
    "kind.x-k8s.io/registry=localhost:${REGISTRY_PORT}"
done

echo ""
echo "✅ Cluster '${CLUSTER_NAME}' ready."
echo "   Registry : localhost:${REGISTRY_PORT}"
echo "   Context  : kind-${CLUSTER_NAME}"
echo ""
echo "Next: make helm-deps && make dev"
