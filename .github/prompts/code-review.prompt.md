---
mode: ask
description: Review code changes before raising a PR
tools: [codebase, terminal, githubRepo]
---

Review the staged/unstaged changes in this branch and check for issues.

## What to check

### Correctness
- Does the logic match the intent described in the linked issue?
- Are all `Result<T>` failure paths handled and returned (not thrown)?
- Are all new `async` methods awaited?
- Is `CancellationToken` passed through the call chain?

### Architecture violations
- ❌ Domain layer must have zero package dependencies
- ❌ Application layer must not reference Infrastructure
- ❌ No `new` handler instances in production code — always dispatch via `sender.SendAsync(...)`
- ❌ No `DateTime.UtcNow` — must use injected `TimeProvider`
- ❌ No `[ApiController]` / `ControllerBase` — Minimal APIs only

### Tests
- Is there a unit test for every new handler?
- Is every error path covered by at least one test?
- Do integration tests call `ResetDatabaseAsync()` in `InitializeAsync`?

### Build quality
Run and confirm clean:
```bash
make build    # zero warnings, zero errors
make test     # all unit tests green
```

### Security
- No secrets, tokens, or credentials committed
- No SQL string concatenation (use EF Core parameterised queries)
- New endpoints have appropriate validation via FluentValidation

## Output format
List only real issues with file + line reference. Do not flag style or formatting.
