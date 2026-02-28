#!/usr/bin/env bash
# Runs EF Core migrations against the PostgreSQL instance running in the local kind cluster.
# Port-forwards the service temporarily, applies migrations, then cleans up.
set -euo pipefail

NAMESPACE="${NAMESPACE:-default}"
LOCAL_PORT="15432"  # Non-standard port avoids conflicts with any local postgres

echo "→ Port-forwarding postgresql service to localhost:${LOCAL_PORT}..."
kubectl port-forward --namespace "${NAMESPACE}" svc/postgresql "${LOCAL_PORT}":5432 &
PF_PID=$!
trap 'kill "${PF_PID}" 2>/dev/null || true' EXIT

# Wait for the port-forward to be ready
sleep 3

CONNECTION="Host=localhost;Port=${LOCAL_PORT};Database=servicetemplate;Username=postgres;Password=postgres"

echo "→ Applying EF Core migrations..."
dotnet ef database update \
  --project src/Infrastructure \
  --startup-project src/Api \
  --connection "${CONNECTION}"

echo "✅ Migrations applied."
