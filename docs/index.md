# .NET 10 service template

A production-ready .NET 10 service template with Clean Architecture, Docker,
Helm and GitHub Actions CI/CD. devhub's **New .NET service** template creates
new repositories from it.

## Architecture

| Layer             | Contains                                               |
| ----------------- | ------------------------------------------------------ |
| Api (entry point) | Minimal APIs, middleware, DI wiring                    |
| Application       | CQRS (`ISender` / `IRequestHandler`), validators, DTOs |
| Domain            | Entities, value objects, domain errors                 |
| Infrastructure    | EF Core, PostgreSQL, repositories                      |

Key decisions:

- **Custom CQRS** — commands and queries dispatched via `ISender.SendAsync()`,
  with pipeline behaviors for logging and validation
- **`Result<T>`** — no exceptions for expected failures; errors flow as values
- **Testcontainers** — integration and acceptance tests run against real
  PostgreSQL containers
- **OpenTelemetry** — traces, metrics and logs exported via OTLP
- **Serilog** — structured logging, with Seq for local development

Setup, project structure, testing, Docker, Helm, CI/CD and renaming are
covered in the
[README](https://github.com/dever-labs/dotnet10-service-template#readme).

## Guides

- [GitHub organisation security policy](github-actions-org-policy.md) — the
  access, validation, supply-chain and audit controls the CI/CD setup assumes
