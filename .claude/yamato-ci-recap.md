# Yamato CI/CD Pipeline Recap

> Generated 2026-04-23 from `.yamato/` configuration files.

## Template Variables (common.metafile)

- **releases**: `public`, `internal`, `staging` — most jobs are templated across all three
- **environments** (deploy): `test` (--test), `prod`
- **modes** (deploy): default, `dry-run`
- **images**: macOS 13 (m1.mac), Windows 11, Ubuntu 22.04, Windows VS2019 (codesign)
- **spec_files**: 19 OpenAPI specs (Identity, CloudCode, Lobby, GSH, CDN, Economy, Leaderboard, Auth, Access, Triggers, Scheduler, CloudSave, Matchmaker, etc.)
- **nuget_publish_envs**: `test` (--nuget-test-publish), `prod` (--nuget-publish)

---

## Jobs

### 1. Triggers (`trigger.yml`)

| Job ID | Description | Trigger |
|---|---|---|
| `main_trigger-{release}-tests` | Push to main trigger | Push to `main` branch (changes in `Unity.Services.Cli/**`, ignoring markdown-only) |
| `pr_trigger-{release}-tests` | PR trigger | Any non-draft PR |

Both fan out to unit tests (mac/ubuntu/windows) and integration tests (mac/ubuntu/windows) for each release type.

### 2. Unit Tests (`unit-test.yml`)

| Job ID | Platforms | Notes |
|---|---|---|
| `unit-test-mac-{release}` | macOS | Depends on download-node |
| `unit-test-ubuntu-{release}` | Ubuntu | Removes old Node.js first |
| `unit-test-windows-{release}` | Windows | Installs OpenJDK 21 via choco |
| `pr-unit-test-windows-{release}` | Windows (PR variant) | Same as above, used in PR trigger |

- For `public` release type: runs `starbuck2` to strip feature flags before testing.
- All set `DOTNET_CLI_TELEMETRY_OPTOUT=1`.
- Filter: `--filter "UnitTest"` with `test.runsettings`.

### 3. Integration Tests (`integration-test.yml`)

| Job ID | Platforms | Notes |
|---|---|---|
| `integration-test-mac-{release}` | macOS | |
| `integration-test-ubuntu-{release}` | Ubuntu | Removes old Node.js |
| `integration-test-windows-{release}` | Windows | 60s timeout on test step |

- Same starbuck2 feature stripping for `public`.
- Filter: `--filter "IntegrationTest"`.
- Depends on `download-node`.

### 4. Download Node.js (`download-resource.yml`)

| Job ID | Description |
|---|---|
| `download-node` | Downloads Node.js v15.0.0 for macOS, Linux, and Windows |

Artifacts placed in `.tmp/MacNode`, `.tmp/LinuxNode`, `.tmp/WinNode`.

### 5. Build (`build.yml`)

| Job ID | Description |
|---|---|
| `build-binaries-{release}` | Builds CLI binaries on Linux |

- `public`: strips features via starbuck2, adds `ENABLE_UGS_CLI_TELEMETRY` define.
- `staging`: adds `USE_STAGING_ENDPOINTS` define.
- `internal`: no extra defines.
- Runs `./build.py` (multi-platform cross-compilation).
- Generates `job-build.json` for codesign using `YAMATO_JOB_ID`, `YAMATO_PROJECT_NAME`, `YAMATO_TOKEN`.

### 6. Codesign (`codesign.yml`)

| Job ID | Platform | Description |
|---|---|---|
| `codesign-mac-{release}` | macOS | Apple codesign + notarization |
| `codesign-windows-{release}` | Windows | Azure Key Vault EV codesign |

**Mac codesign pipeline:**
1. Fetches signing certificate from Azure Key Vault (`unity-cs-kv-euw1-prd.vault.azure.net`)
2. Unlocks keychain, runs `codesign` with entitlements
3. Creates zip, submits to Apple notarization via `xcrun notarytool`
4. Verifies notarization result

**Windows codesign pipeline:**
1. Uses Azure Key Vault EV certificate (`ev-unity-technologies-sf`)
2. Signs via `powershell.cds.ci.code-signing` brick

Depends on `build-binaries-{release}`.

### 7. Strip Features (`strip-features.yml`)

| Job ID | Description |
|---|---|
| `strip-features` | Strips feature-flagged code using `starbuck2` tool |

Standalone job (most jobs inline the stripping instead of depending on this).

### 8. Prepare Release (`prepare-release.yml`)

| Job ID | Description |
|---|---|
| `prepare-release-binary-{release}` | Assembles signed binaries for all platforms |

- Runs on macOS (M1).
- Extracts signed macOS zip, copies signed Windows exe, copies Linux binaries.
- Smoke-tests macOS binary (`ugs -h`).
- Depends on: build, codesign (mac + windows), all unit tests, all integration tests.

### 9. Deploy (`deploy.yml`)

| Job ID | Description |
|---|---|
| `deploy-{release}:{env}` | Deploy to Artifactory |
| `deploy-{release}:{env}:dry-run` | Dry-run deploy |

- Installs `parcel` v0.0.38 and ships to apt/brew/choco.
- Environments: `test`, `prod`.
- Depends on `prepare-release-binary-{release}`.

### 10. NuGet Publish (`nuget_publish.yml`)

| Job ID | Description |
|---|---|
| `publish_{spec}_to_nuget_{env}` | Publishes OpenAPI-generated NuGet packages |

- One job per spec file (19) x environment (test/prod) = 38 jobs.
- Installs maven, jq, openjdk, nvm/node 22, python venv.
- Authenticates to both github.com and github.cds.internal.unity3d.com.
- Runs `OpenApi/openapi_generator.py` with publish flags.

### 11. Download OpenAPI Specs (`download-specs.yml`)

| Job ID | Description |
|---|---|
| `update-openapi-specs` | Downloads latest OpenAPI specs and creates a commit |

- Installs GitHub CLI, authenticates to github.cds.internal.unity3d.com.
- Runs `Tools/MockServer/download_external_specs.py`.
- Auto-commits updated specs and pushes.

### 12. Auto Release (`auto-release.yml`)

| Job ID | Description | Trigger |
|---|---|---|
| `weekly_github_release` | Weekly release orchestrator | Manual (depends on bump job, rerun: always) |
| `bump_cli_internal_release_version` | Bumps version, updates CHANGELOG, creates PR | No trigger (called by weekly) |
| `release_cli_internally` | Creates GitHub release with all binaries | PR from `github-auto-bump/*` to `main` (non-draft) |

**Bump flow:**
1. Authenticates to github.cds.internal.unity3d.com
2. Checks for "pause" label via `error_if_gh_issue_with_label.py`
3. Bumps version via `project-version-bump.py --bump-mode internal`
4. Updates CHANGELOG, pushes branch, creates PR

**Release flow:**
1. Authenticates to github.cds.internal.unity3d.com
2. Renames binaries (public/internal/staging x linux/linux-musl/macos/windows = 12 artifacts)
3. Creates GitHub prerelease and uploads all 12 binaries

---

## Secrets & Environment Variables

### Secrets (sensitive, set in Yamato CI)

| Secret | Used In | Purpose |
|---|---|---|
| `GITHUB_OAUTH_TOKEN` | download-specs, nuget_publish | GitHub.com OAuth token |
| `GITHUB_CDS_PR_CREATE_AUTH_TOKEN` | download-specs, nuget_publish, auto-release (bump) | GitHub CDS (internal) token for creating PRs and authenticating |
| `GITHUB_CDS_PR_APPROVE_AUTH_TOKEN` | auto-release (release) | GitHub CDS token for approving/releasing |
| `UNITY_KEYCHAIN_PASSWORD` | codesign-mac | macOS keychain unlock password |
| `NOTARIZATION_TEAM_ID` | codesign-mac | Apple notarization team ID |
| `NOTARIZATION_APP_SPECIFIC_PASSWORD` | codesign-mac | Apple app-specific password for notarization |
| `YAMATO_TOKEN` | build | Yamato API key (used in job-build.json for codesign) |
| `INTEGRATION_TEST_USER_EMAIL` | download-specs | Git user email for auto-commits |
| `YAMATO_OWNER_EMAIL` | auto-release (bump) | Git user email for version bump commits |

### Commented-out / Future Secrets

| Secret | Notes |
|---|---|
| `MAC_NOTARIZE_APPLE_ID` | Commented out in codesign.yml — "new" credentials |
| `MAC_NOTARIZE_TEAM_ID` | Commented out in codesign.yml |
| `MAC_NOTARIZE_APP_PASSWORD` | Commented out in codesign.yml |

### Azure Key Vault (accessed via brick, not env vars)

| Resource | Used In | Purpose |
|---|---|---|
| `unity-cs-kv-euw1-prd.vault.azure.net` | codesign-mac, codesign-windows | Azure Key Vault for signing certificates |
| `apple-developer-id-application-unity-technologies-sf` | codesign-mac | Apple Developer ID certificate name |
| `ev-unity-technologies-sf` | codesign-windows | EV code signing certificate name |
| Azure Tenant ID `45b9a1d4-a8af-40da-8eca-96bebddf6fc7` | codesign-mac | Azure AD tenant |

### Non-Secret Variables

| Variable | Used In | Purpose |
|---|---|---|
| `DOTNET_CLI_TELEMETRY_OPTOUT=1` | unit-test, integration-test | Disables .NET telemetry |
| `EXTRA_CLI_DEFINES` | build | Build-time defines (`ENABLE_UGS_CLI_TELEMETRY`, `USE_STAGING_ENDPOINTS`) |
| `OPENAPI_GENERATOR_VERSION=6.2.0` | nuget_publish | OpenAPI generator version |
| `YAMATO_JOB_ID` | build, auto-release | Yamato-provided job ID |
| `YAMATO_PROJECT_NAME` | build | Yamato-provided project name |
| `GIT_BRANCH` | download-specs, auto-release | Target branch for git operations |
| `ARTIFACTS_ZIPLINK_FILEPATH` | codesign | Path for codesign input file list |

---

## Pipeline Flow (simplified)

```
PR opened / Push to main
  |
  +---> Unit Tests (mac/ubuntu/windows) x (public/internal/staging)
  +---> Integration Tests (mac/ubuntu/windows) x (public/internal/staging)
          |
          +---> Build Binaries x (public/internal/staging)
                  |
                  +---> Codesign Mac + Codesign Windows
                          |
                          +---> Prepare Release Binaries
                                  |
                                  +---> Deploy to Artifactory (test/prod) [manual]

Weekly (auto-release):
  bump version --> create PR --> (PR trigger: build+test+sign) --> release to GitHub
```
