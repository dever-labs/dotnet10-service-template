## Summary
<!-- One or two sentences describing what this PR does and why. -->


## Type of change
- [ ] Bug fix
- [ ] New feature
- [ ] Breaking change (migration required / API contract change)
- [ ] Refactoring / tech debt
- [ ] Documentation / configuration

## Related issue
Closes #<!-- issue number -->

## What changed
<!-- List the key files and what was changed in each. This helps reviewers and the AI review tool. -->
- `src/Domain/...` —
- `src/Application/...` —
- `src/Api/...` —
- `tests/...` —

## How to test
<!-- Steps to verify the change works correctly. -->
1.
2.

## Checklist
- [ ] `make build` passes — zero warnings, zero errors
- [ ] `make test` passes — all unit tests green
- [ ] New/changed handlers have unit tests covering happy path and all error paths
- [ ] No exceptions thrown for expected failures — `Result<T>` used throughout
- [ ] No secrets or credentials committed
- [ ] Migration created if schema changed (`make migration NAME=...`)
- [ ] Documentation updated if behaviour changed

