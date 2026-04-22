# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

All commands run from the repo root unless noted. The solution lives in `Unity.Services.Cli/`.

### Build

```bash
# Build the solution
dotnet build Unity.Services.Cli/Unity.Services.Cli.sln

# Multi-platform release build (Windows, macOS, Linux, Alpine → outputs to builds/)
python build.py

# Build with feature flags (e.g. staging endpoints)
python build.py --extra-defines USE_STAGING_ENDPOINTS

# Skip specific platform builds
SKIP_LINUX_BUILD=1 SKIP_WINDOWS_BUILD=1 python build.py
```

### Test

```bash
# Run all unit tests
dotnet test Unity.Services.Cli/Unity.Services.Cli.sln --filter "UnitTest"

# Run a single test project
dotnet test Unity.Services.Cli/Unity.Services.Cli.<Module>.UnitTest/

# Run with test result logging
dotnet test Unity.Services.Cli/Unity.Services.Cli.sln --filter "UnitTest" --logger "trx;LogFileName=test-results.trx"
```

### Format / Lint

```bash
# Install git hooks (required once per checkout)
npm i

# Format C# files (also runs automatically via pre-commit hook)
dotnet format Unity.Services.Cli/Unity.Services.Cli.sln
```

### New Module Scaffolding

Run from inside `Unity.Services.Cli/`:

```bash
# Install templates (one-time)
dotnet new --install Unity.Services.Cli.ModuleTemplate
dotnet new --install Unity.Services.ModuleTemplate.Authoring.Core
dotnet new --install Unity.Services.Cli.ModuleTemplate.UnitTest

# Create new module projects
dotnet new UgsCliModule --name <Module> --output Unity.Services.Cli.<Module>
dotnet new UgsCliModuleDeploy --name <Module> --output Unity.Services.<Module>.Authoring.Core
dotnet new UgsCliModuleUnitTest --name <Module> --output Unity.Services.Cli.<Module>.UnitTest
```

## Architecture

The CLI is a modular .NET 8 application (~76 projects in `Unity.Services.Cli/Unity.Services.Cli.sln`). Each Unity Gaming Service (Cloud Code, Economy, Leaderboards, etc.) is an independent module project.

### Key Projects

| Project | Role |
|---|---|
| `Unity.Services.Cli` | Entry point; wires all modules into the host |
| `Unity.Services.Cli.Common` | Shared abstractions: `ICommandModule`, `CommonInput`, DI helpers |
| `Unity.Services.Cli.<Module>` | Per-service module (commands + DI registration) |
| `Unity.Services.<Module>.Authoring.Core` | Deployment/config-as-code logic, no CLI dependency |
| `Unity.Services.Cli.<Module>.UnitTest` | NUnit unit tests for a module |
| `Unity.Services.Cli.IntegrationTest` | Integration tests using WireMock.Net mock server |

### Adding a Command to a Module

1. **Module class** implements `ICommandModule` (in `Unity.Services.Cli.Common`).
2. **Commands** are `System.CommandLine.Command` objects; handlers are async methods.
3. **Input** classes inherit `CommonInput`; properties use `[InputBinding]` attribute to bind to `Option`/`Argument`.
4. **Register** in `Program.cs` via `.AddModule(new YourModule())` and `.ConfigureServices(YourModule.RegisterServices)`.
5. **Services** exposed to other modules are registered as singletons in `RegisterServices(HostBuilderContext, IServiceCollection)`.

### Service Clients

HTTP clients are generated from OpenAPI specs stored in `OpenApi/`. Generated code lands in `.tmp` subdirectories inside module projects. See `OpenApi/README.md` for regeneration steps.

### Feature Flags

Unfinished features must be hidden behind a feature flag. Flags are defined in `features-definition.json` and enabled at build time via `DefineConstants` in the `.csproj`. Wrap unreleased code with `#if FEATURE_<NAME>`.

### Integration Tests

Integration tests build with `USE_MOCKSERVER_ENDPOINTS` define and spin up `Unity.Services.Cli.Integration.MockServerApp` (WireMock.Net). Run them separately from unit tests; CI only runs the `--filter "UnitTest"` suite on PRs.

## Pull Requests

Always use the template at `.github/pull_request_template.md` when creating PRs.

## Code Conventions

Enforced as compiler errors via `.editorconfig`:

| Field kind | Naming |
|---|---|
| Private constants | `k_PascalCase` |
| Private instance fields | `m_PascalCase` |
| Private static fields | `s_PascalCase` |
| Private static readonly | `k_PascalCase` |

- All warnings are treated as errors (`TreatWarningsAsErrors = true`).
- `using` directives go **outside** the namespace.
- Allman brace style (new line before `{` everywhere).
- `.csproj`/`.props`/`.targets` files use 2-space indent, CRLF line endings, UTF-8 BOM.
