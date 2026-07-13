# Iterate on a CLI Command (NuGet Dual-Consumer Pattern)

Use this command when adding or fixing a feature in a CLI module that shares logic with a Unity Editor plugin via a NuGet package.

---

## 0. Clarify before starting

**Before doing any work, ask the user:**

1. **Jira ticket** — What is the associated Jira ticket ID (e.g. `ULO-1234`)?
2. **Module / service** — Which service does this affect (e.g. `CloudCode`, `Economy`, `Leaderboards`)?
3. **Fix or feature?** — Is this a `fix` or a `feat`?
4. **Short description** — One brief kebab-case phrase for the branch name (e.g. `add-new-file-command`).
5. **Any required env vars?** — Does the command need environment variables (e.g. service keys, project IDs)?
   - If yes: ask the user to **set them in the shell before starting the session** or pass them inline before commands.
   - **Do NOT store, log, or persist any env var values.** Never write credentials to files, memory, or history.

Use the answers to construct the branch name: `<module-shortname>/(feat|fix)/ULO-<ticket>-<short-description>`
Examples: `cloudcode/fix/ULO-8694-fix-ccm-template`, `economy/feat/ULO-1234-add-publish-command`

---

## 1. Understand the dual-consumer architecture

Each Unity service (Cloud Code, Economy, etc.) shares its authoring logic between:
- **Unity Editor plugin** — has direct filesystem access to the SDK source tree
- **CLI** — ships as a binary and must extract embedded resources from the NuGet DLL at runtime

The same `Authoring.Editor.Core` assembly is consumed by both. Any shared logic must work in both contexts.

**Cloud Code only**: has a module template (`new-file` command). Template files live in the SDK repository under `Packages\Public\com.unity.services.cloudcode\Editor\Authoring\Core\Solution\Template~\`. The Editor plugin accesses them directly from disk; the CLI extracts them from the NuGet DLL into a temp directory. Other services share logic but do not have a template.

---

## 2. Find the NuGet source project

In the **SDK repository**:
```
Packages\Public\com.unity.services.<service>\NuGet~\Unity.Services.<service>.Authoring.Editor.Core.csproj
```

This builds the NuGet package. Inspect it to understand:
- What files are embedded as resources
- The current package version
- The root namespace (= embedded resource name prefix)

---

## 3. Find the CLI exe path

Check `Unity.Services.Cli\Unity.Services.Cli\Unity.Services.Cli.csproj` for the `AssemblyName` and `TargetFramework`.

Default assumed path (verify against csproj):
```
Unity.Services.Cli\Unity.Services.Cli\bin\Debug\net10.0\ugs.exe
```

---

## 4. Local test workflow (before NuGet publish)

To test NuGet changes without publishing:

1. In the CLI module's `.csproj`, temporarily replace:
   ```xml
   <PackageReference Include="Unity.Services.<service>.Authoring.Editor.Core" Version="x.y.z" />
   ```
   with a `ProjectReference` pointing to the NuGet source `.csproj` in the SDK repository.

2. Build the CLI solution:
   ```
   dotnet build Unity.Services.Cli/Unity.Services.Cli.sln
   ```

3. Run the output binary directly (don't use `dotnet run`):
   ```
   Unity.Services.Cli\Unity.Services.Cli\bin\Debug\net10.0\ugs.exe <command>
   ```
   If env vars are needed, pass them inline: `VAR=value ./ugs.exe <command>`

4. **Never commit the `ProjectReference` change** — stage only the files you intend to ship.

Once the NuGet is published, bump the `PackageReference` version in the CLI `.csproj`.

---

## 5. Discover existing commands

`commands.api` at the CLI repo root lists all CLI commands and their signatures. Check it before adding a new command to avoid duplication.

---

## 6. Checklist

- [ ] Branch created: `<module-shortname>/(feat|fix)/ULO-<ticket>-<short-description>`
- [ ] NuGet version bumped appropriately (patch for fix, minor for new feature)
- [ ] CLI module updated to consume new NuGet behavior
- [ ] Unit tests updated or added
- [ ] `CHANGELOG.md` updated under `[Unreleased]`
- [ ] `commands.api` updated if command signatures changed
- [ ] Local `ProjectReference` removed before committing
