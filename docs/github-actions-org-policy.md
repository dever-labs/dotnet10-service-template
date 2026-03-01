# GitHub Organisation Security Policy

This document describes the full security, quality, and software validation posture for the `dever-labs` GitHub organisation. It is written for organisations operating critical infrastructure where software defects, supply chain attacks, or unauthorised changes can have serious consequences.

The controls described here span four concerns:

1. **Access & identity** — who can do what, and how that is verified
2. **Code quality & validation** — what must pass before code reaches production
3. **Supply chain security** — protecting against compromised dependencies and actions
4. **Audit & compliance** — maintaining evidence that controls are in place and working

---

## 1. Identity & Access

### 1.1 SSO and MFA

> **Organisation Settings → Authentication security**

| Setting | Value | Reason |
|---|---|---|
| Require SAML single sign-on | ✅ Enabled | All access flows through the identity provider (IdP) — revoking IdP access immediately revokes GitHub access |
| Require two-factor authentication | ✅ Enabled | Fallback protection if SSO is bypassed or during bootstrap |

When SSO is enforced, any member who has not authenticated via the IdP loses access to private repos automatically. Offboarding is instant — disable the account in the IdP, access is gone everywhere.

### 1.2 IP Allowlisting

> **Organisation Settings → IP allow list**

Restrict API and web access to known corporate IP ranges and VPN egress IPs. This prevents credentials from being used outside approved networks even if they are leaked.

### 1.3 Personal Access Tokens

> **Organisation Settings → Personal access tokens**

| Setting | Value |
|---|---|
| Restrict personal access token access | ✅ Enabled |
| Require approval for fine-grained PATs | ✅ Enabled |
| Allow classic tokens | ❌ Disabled |

Classic PATs are organisation-wide and cannot be scoped. Fine-grained PATs are scoped to specific repos and expire. Require approval so tokens are visible to org admins before they are used.

---

## 2. Repository Standards

These settings apply across all repositories and are enforced via **Organisation Rulesets** rather than per-repo branch protection rules. Rulesets apply automatically to new repos and cannot be overridden by repo admins.

> **Organisation Settings → Rules → Rulesets**

### 2.1 Default Ruleset — All Repositories

Apply to: all repositories, targeting the `main` branch (and `release/*` if used).

| Rule | Setting |
|---|---|
| Restrict deletions | ✅ |
| Require linear history | ✅ — no merge commits, squash or rebase only |
| Require signed commits | ✅ — see section 2.2 |
| Require a pull request before merging | ✅ |
| Required approvals | Minimum **2** for production repos, **1** for internal tooling |
| Dismiss stale reviews on push | ✅ |
| Require review from code owners | ✅ |
| Require status checks to pass | ✅ — see section 3 |
| Block force pushes | ✅ |
| Require deployments to succeed | ✅ for staging environment before merging to main |

### 2.2 Commit Signing

All commits must be signed with a verified GPG or SSH key, or via GitHub's Vigilant Mode (web editor, GitHub Desktop).

```bash
# Configure GPG signing locally
git config --global commit.gpgsign true
git config --global user.signingkey <YOUR_KEY_ID>
```

Signed commits give a chain of custody: every commit has a verified author, making it impossible for an attacker to silently inject commits even with repository write access.

### 2.3 CODEOWNERS

Every repository must define a `CODEOWNERS` file:

```
# .github/CODEOWNERS

# Default — all files require review from the platform team
*                           @dever-labs/platform

# Security-sensitive paths require the security team in addition
.github/workflows/          @dever-labs/platform @dever-labs/security
src/Infrastructure/         @dever-labs/platform @dever-labs/security
deploy/                     @dever-labs/platform @dever-labs/security
```

This ensures that changes to CI workflows, infrastructure code, and deployment configuration always require explicit sign-off from the right people — not just any approved reviewer.

---

## 3. Required Status Checks

The following checks must pass on every PR before merge. Configure these as required in the Ruleset under **Require status checks**.

| Check | What it validates |
|---|---|
| `build` | Solution compiles with `TreatWarningsAsErrors=true` |
| `unit-tests` | All unit tests pass |
| `integration-tests` | All integration tests pass |
| `acceptance-tests` | All acceptance tests pass |
| `format` | Code is correctly formatted (`dotnet format --verify-no-changes`) |
| `CodeQL` | No high/critical code vulnerabilities |
| `dependency-review` | No newly introduced high/critical CVEs in dependencies |
| `container-scan` | Docker image passes Trivy scan with no fixable critical CVEs |

Checks are not advisory — they are hard gates. A PR cannot merge if any required check is failing regardless of approvals.

---

## 4. GitHub Actions Policy

> **Organisation Settings → Actions → General**

### 4.1 Permitted Actions

Select: **Allow dever-labs, and select non-dever-labs, actions and reusable workflows**

| Option | Enable |
|---|---|
| Allow actions created by GitHub | ✅ |
| Allow actions by Marketplace verified creators | ✅ |
| Require actions to be pinned to a full-length commit SHA | ✅ |

Only verified Marketplace creators are permitted. Unknown or unverified actions are blocked at the org level before any code executes.

### 4.2 SHA Pinning

All workflow action references must use a full commit SHA, not a tag. Tags are mutable and can be redirected to malicious code. SHAs are immutable.

```yaml
# ❌ Tag — can be moved or hijacked
- uses: actions/checkout@v4

# ✅ SHA — immutable reference
- uses: actions/checkout@11bd71901bbe5b1630ceea73d27597364c9af683 # v4
```

Use [`pin-github-action`](https://github.com/mheap/pin-github-action) for a one-time bulk conversion across all repos. Dependabot keeps SHAs current automatically.

### 4.3 Workflow Permissions

> **Organisation Settings → Actions → General → Workflow permissions**

| Setting | Value |
|---|---|
| Default permissions | **Read repository contents** only |
| Allow GitHub Actions to create and approve pull requests | ❌ Disabled |

Workflows must explicitly request elevated permissions using the `permissions:` key. This follows the principle of least privilege — a compromised workflow cannot write to the repo, create releases, or push packages unless it was explicitly granted that right.

```yaml
# Explicitly declare only what is needed
permissions:
  contents: read
  packages: write
```

### 4.4 Environment Protection Rules

Production and staging deployments must use GitHub Environments with protection rules:

> **Repository Settings → Environments**

| Environment | Required reviewers | Deployment branch | Wait timer |
|---|---|---|---|
| `staging` | 0 (automated) | `main` only | — |
| `production` | — | Tag `v*.*.*` only | — |

Production is **not deployed from this repo** — it is managed via the GitOps repository. The tag-based release workflow publishes the artefacts (container image, Helm chart); the GitOps repo controls when they reach production.

---

## 5. Supply Chain Security

### 5.1 Dependabot — Dependencies and Actions

Every repository must include a `dependabot.yml` that covers both NuGet packages and GitHub Actions:

```yaml
version: 2
updates:
  - package-ecosystem: nuget
    directory: /
    schedule:
      interval: weekly
    open-pull-requests-limit: 10

  - package-ecosystem: github-actions
    directory: /
    schedule:
      interval: weekly
    open-pull-requests-limit: 10
```

Dependabot opens PRs automatically. Required status checks run on those PRs — Dependabot updates only merge when the build and tests pass.

### 5.2 Dependency Review

The `dependency-review-action` runs on every PR and blocks merging if new dependencies introduce known CVEs above a configurable severity threshold:

```yaml
- uses: actions/dependency-review-action@<SHA>
  with:
    fail-on-severity: high
```

This catches vulnerable packages at the point they are introduced — before they ever reach `main`.

### 5.3 Secret Scanning

> **Organisation Settings → Code security → Secret scanning**

| Setting | Enable |
|---|---|
| Secret scanning | ✅ |
| Push protection | ✅ |
| Validity checks | ✅ |
| Non-provider patterns | ✅ |

Push protection blocks the push at the client before a secret ever reaches GitHub. It does not wait for a scan after the fact.

### 5.4 Container Image Signing

All container images published to GHCR must be signed using [Sigstore Cosign](https://github.com/sigstore/cosign). This proves the image was produced by the official CI pipeline and has not been tampered with.

```yaml
- name: Sign container image
  uses: sigstore/cosign-installer@<SHA>

- name: Sign the image
  run: |
    cosign sign --yes \
      ${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}@${{ steps.build.outputs.digest }}
  env:
    COSIGN_EXPERIMENTAL: true
```

Consumers can verify before deploying:

```bash
cosign verify ghcr.io/dever-labs/my-service@sha256:<digest> \
  --certificate-identity-regexp="https://github.com/dever-labs/.*" \
  --certificate-oidc-issuer="https://token.actions.githubusercontent.com"
```

### 5.5 SBOM Generation

Every release must produce a Software Bill of Materials. The `docker/build-push-action` generates this automatically when `sbom: true` is set:

```yaml
- uses: docker/build-push-action@<SHA>
  with:
    sbom: true
    provenance: true
```

The SBOM is attached to the image manifest in GHCR and provides a complete inventory of every package in the image, enabling rapid impact assessment when new CVEs are published.

### 5.6 NuGet Package Source Control

All NuGet packages must flow through the organisation's internal proxy feed. The proxy:

- Mirrors approved packages from nuget.org
- Blocks packages that have not been reviewed
- Provides a single point to quarantine a compromised package across all repos simultaneously

```xml
<!-- nuget.config -->
<packageSources>
  <clear />
  <add key="org-nuget-proxy" value="https://nuget.internal/v3/index.json" />
</packageSources>
```

---

## 6. Code Scanning

### 6.1 CodeQL

> **Organisation Settings → Code security → Code scanning**

Enable CodeQL for all repositories at the organisation level. For .NET, use the `csharp` language pack with the `security-extended` query suite:

```yaml
- uses: github/codeql-action/init@<SHA>
  with:
    languages: csharp
    queries: security-extended
```

CodeQL catches common vulnerability classes including SQL injection, path traversal, insecure deserialization, and improper input validation. Results appear in the **Security** tab and block merging when configured as a required check.

### 6.2 Container Scanning

Trivy scans the built container image for OS-level and application CVEs before it is pushed:

```yaml
- name: Scan container image
  uses: aquasecurity/trivy-action@<SHA>
  with:
    image-ref: ${{ env.IMAGE }}
    format: sarif
    output: trivy-results.sarif
    severity: CRITICAL,HIGH
    exit-code: 1          # fail the build on fixable findings

- name: Upload Trivy results
  uses: github/codeql-action/upload-sarif@<SHA>
  with:
    sarif_file: trivy-results.sarif
```

Results appear in the repository Security tab alongside CodeQL findings.

---

## 7. Audit & Compliance

### 7.1 Organisation Audit Log

> **Organisation Settings → Audit log**

The audit log records every significant event: repo creation, permission changes, branch protection changes, secrets access, and workflow runs. For critical infrastructure, stream the audit log to your SIEM:

> **Organisation Settings → Audit log → Log streaming**

Supported targets include Splunk, Azure Event Hubs, Amazon S3, and Datadog. This ensures the log cannot be tampered with even by org admins.

### 7.2 Build Provenance

The `docker/build-push-action` with `provenance: true` generates a signed SLSA provenance attestation alongside the image. This proves:

- Which commit triggered the build
- Which workflow ran it
- Which runner executed it
- That no modifications were made between source and image

### 7.3 Release Evidence

Every tagged release should produce a GitHub Release with:

- Auto-generated release notes (from PR titles and conventional commits)
- Docker image digest
- Helm chart version
- SBOM attachment
- Provenance attestation link

This forms the traceability record connecting a deployed artefact back to the exact source commit, PR, and approvals that produced it.

---

## 8. Keeping Controls Current

| Control | How it stays up to date |
|---|---|
| Action SHAs | Dependabot weekly PRs |
| NuGet packages | Dependabot weekly PRs |
| Base container images | Dependabot (Docker ecosystem) weekly PRs |
| CodeQL query suite | GitHub manages automatically |
| CVE database (Trivy) | Trivy pulls latest DB on each run |
| Org Rulesets | Reviewed quarterly by platform team |
| This document | Updated when policies change — kept alongside the code it describes |

---

## Summary

| Layer | Control |
|---|---|
| Identity | SSO + MFA, IP allowlist, fine-grained PATs only |
| Repository | Org Rulesets, signed commits, CODEOWNERS, linear history |
| Code quality | Build, test, format gates on every PR |
| Vulnerability detection | CodeQL, Trivy, dependency review, secret scanning with push protection |
| Supply chain | SHA-pinned actions, signed images, SBOM, provenance, NuGet proxy |
| Audit | Org audit log streaming, SLSA provenance, release evidence trail |

