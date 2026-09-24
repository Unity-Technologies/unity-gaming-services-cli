# Mirror of com.unity.services.shared authoring template

**This directory is a mirror. Do not edit in isolation.**

Upstream source of the pattern (in a separate repo):
`operate-services-sdk/Packages/Internal/com.unity.services.shared/Editor/Authoring/Core/`
(GitHub Enterprise: https://github.cds.internal.unity3d.com/unity/operate-services-sdk)

## Rule

Any change to a file in this directory MUST be paired with the equivalent change on the upstream side, filed as a companion PR. Reviewers on both sides should refuse to merge without the sibling PR link in the description.

Both sides carry an `AGENTS.md` (with a `CLAUDE.md` stub `@AGENTS.md` for Claude Code). Content is symmetric. If you're reading this on the CLI side, the upstream side has the mirror rule too.

## Namespace rewrite

Upstream `Unity.Services.MyService.Editor.Authoring.Core.*` <-> CLI `Unity.Services.ModuleTemplate.Authoring.Core.*` (drops `.Editor`, renames `MyService` -> `ModuleTemplate`).

## CLI-side adaptations to preserve

When porting FROM upstream, preserve these differences:
- No `ILogger` in constructors (CLI stripped `Logger/`)
- Interfaces extracted to their own `I*.cs` files
- `DeployResult` / `FetchResult` in place of `DeploymentResult<T>`
- `AssetState` 3-arg ctor only (CLI's `Unity.Services.DeploymentApi` is 1.0.0-beta.5)
- `Validate()` requires Id only (test fixture convention here is Id-only)
- CLI carries `CompoundResourceDeploymentItem` / `NestedResourceDeploymentItem` for a compound-file pipeline that upstream doesn't have; upstream keeps only the simple pipeline. Do not delete the compound classes when syncing FROM upstream.

When pushing TO upstream, keep changes additive - upstream is a shipped Unity Editor package with external consumers. New signatures OK, removed/changed signatures are not.

## Resource abstractions: only introduce when polymorphism actually pays

Both sides intentionally use concrete `SimpleResource` / `SimpleResourceDeploymentItem` (and on this side also `CompoundResourceDeploymentItem` / `NestedResourceDeploymentItem`) directly in handler, loader, and client signatures. Do NOT introduce an `IResource` / `IResourceDeploymentItem` interface layer above them "just in case" a forked template needs alternate implementations - `IScript` in `com.unity.services.cloudcode` earns its keep because that package has 5 concrete impls flowing through one deploy pipeline; a template with one concrete Resource type does not. Reintroduce interfaces only when a second concrete Resource type actually lands in the same pipeline.

## Verify

Both sides must build clean. On the CLI:
- `dotnet build Unity.Services.Cli/Unity.Services.Cli.sln`
- `dotnet test Unity.Services.Cli/Unity.Services.Cli.ModuleTemplate.UnitTest/ --filter UnitTest`

84 unit tests in `Unity.Services.Cli.ModuleTemplate.UnitTest/` exercise this code. Upstream has no dedicated Core tests - this suite is the behavioural coverage for both sides. If you change behaviour, port the change and re-run these tests.

## Do not put user-local paths in this file

`AGENTS.md` and `CLAUDE.md` are checked into git and read on every developer's / agent's machine. Never reference `~/.claude/...`, `%APPDATA%/...`, `C:\Users\<name>\...`, or anything under a per-machine home directory. If context is worth persisting, put it in a repo-tracked doc.
