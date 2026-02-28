---
mode: agent
description: Investigate and fix a failing test or bug
tools: [codebase, terminal]
---

Investigate and fix: **${input:bugDescription}**

## Process

### 1. Reproduce
Run the relevant tests first to see the exact failure:
```
make test           # unit tests
make test-integration  # requires Docker
```

If it's a runtime bug, check logs:
```
make logs
```

### 2. Locate the problem
- Read the stack trace carefully — identify the exact file and line
- Check if the issue is in Domain (business logic), Application (handler/validator), Infrastructure (EF query), or API (endpoint mapping)
- Search for related error codes: the domain uses typed errors in `*Errors.cs` files

### 3. Fix rules
- If the root cause is a missing null check or guard → add it and return the appropriate `Error.*` value — do NOT throw
- If a test is asserting the wrong thing → fix the test to match correct behaviour, not the code to match a wrong test
- If it's an EF Core query issue → check `*Configuration.cs` and the repository implementation
- Do not change unrelated code

### 4. Validate
- Run `make build` — must pass with zero warnings
- Run affected tests — must be green
- If you changed a handler, run its unit tests
- If you changed an endpoint or infrastructure, run integration tests
