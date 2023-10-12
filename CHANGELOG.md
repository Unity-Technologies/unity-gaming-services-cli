# Changelog

All notable changes to UGS CLI will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [1.1.0] - 2023-10-12

### Added
* Bash installer to download and install the UGS CLI on MacOS and Linux
* Added config as code support for economy module
  * Deploy
  * Fetch
* Added config as code support for access module
  * Deploy
  * Fetch
* Added `new-file` commands for economy resources
  * For inventory items
  * For currencies
  * For virtual purchases
  * For real-item purchases
  * For Cloud Code C# Modules
  * For project access policies
  * For triggers
* Added `gsh server files` command behind feature flag
* Added support for .sln files on deploy
  * .sln files now are compiled and zipped into .ccm before deploying
* Added config as code support for triggers
  * Deploy

### Changed
- Services can support multiple file extensions
- Updated server states in `ugs gsh machine list`

### Fixed
- Handle exceptions when using Deploy with a Remote Config file that has unsupported config types.
- Fixed an issue where if a leaderboard fails to load, it incorrectly deploys as a empty leaderboard and it is not reported
- Added correct description when Cloud Code deploy has duplication file error during dry-run.
- Fixed an issue with `ugs gsh fleet-region update` not ensuring the fleet region is brought online by default.
- Handle exception for mis-spelt bool input params for `ugs gsh fleet-region update` command.
- Fixed an issue with Deploy and Fetch on Remote Config containing JSON arrays.

## [1.0.0] - 2023-08-01

### Added
- Added Deployment Definitions to the Deploy and Fetch commands.
- Added analytics related to command usage and options used.
- Deploy/Fetch return an array in a table-like format with -json flag enabled.
- Leaderboards now supports the `ugs deploy` and `ugs fetch` commands at the root 
  - Deploy sends file configurations into the service
  - Fetch updates local files based on service configuration
- Leaderboards now supports `new-file`, to create an empty file for leaderboards
- Added new commands to Game Server Hosting for: machine list

### Changed
- Removed Leaderboards support to `create` and `update` commands.

### Fixed
- GSH Fleet Region Update now properly reports offline fleet region values.
- GSH Fleet Region server settings now align with UDash
- A bug logging an additional error when deploying a file.
- A bug preventing Remote Config deploy from printing entries when encountering a `RemoteConfigDeploymentException`.
- A bug in Cloud Code scripts throwing unhandled exception when using create command with a directory path but an empty file name.

## [1.0.0-beta.6] - 2023-07-10

### Added
- Game Server Hosting Module Service commands. Run `ugs gsh -h` to show usage.
  - Supports builds, build configurations, fleets, fleet regions and servers.

### Fixed
- A bug with the login command when stdin is redirected.
- A bug preventing Remote Config fetch dry run to update the fetched file name.

## [1.0.0-beta.5] - 2023-06-28

### Added
- Added Batching to import and deploy to help prevent "Too Many Requests" error.
- Cloud Code Modules now supports `import` and `export` commands.
- Cloud Code Scripts now supports `import` and `export` commands.
- Lobby now supports `import` and `export` commands.
- Leaderboard now supports `import` and `export` commands.
- Remote Config now supports `import` and `export` commands.
- Alpine build now added to the release.
- New option `--services` to deploy and fetch commands. This option perform commands only to specified services.
  * **[Breaking Change]** This option is mandatory when using the `--reconcile` flag.

### Changed
- **[Breaking Change]** CloudCode `list` command for Modules and Scripts is more descriptive.
- Using standardized output for all Import/Export implementations.
- Plain text Deploy/Fetch Output now prints full path.
  * This is to disambiguate output regarding files with same name, but different path.
- **[Breaking Change]** Messages are directed to StdErr and Output into Stdout.
  * This allows to pipe individual parts such as `ugs cmd 1>output 2>logs.txt`.
  * In both json and regular formats.
- **[Breaking Change]** Cloud Code create, delete, get, list, new-file, publish and update commands are now under a parent command `scripts` and can be called with `cloud-code scripts <command>`.

### Fixed
- CloudCode files that failed to read now reported properly in the output.
- CloudCode deleted files properly reported in the Deploy output.
- RemoteConfig Entries properly reported in the Deploy output.
- RemoteConfig Fetch properly bubbles issues in loading files.
- **[Breaking Change]** Deploy and Fetch output have been modified to match each other.
  * Status have been updated to reflect what is happening in the editor.
- An issue where fetching a file from Cloud Code that had no parameters would keep appending `module.exports.parameters = {}`.
- Using Cloud Code fetch and deploy multiple times does not keep appending new lines anymore.
- Improved error handling to provide more detail on certain unhandled exceptions.
- Cloud Code script with invalid parameters will fail to fetch and show in the "failed" result section.

## [1.0.0-beta.4] - 2023-04-24

### Added

- npm distribution. Install the CLI by running `npm install -g ugs`.
- `ugs fetch` now supports cloud code scripts.
- Get player command in Player Module. Run `ugs player get -h` to show usage.
- List player command in Player Module. Run `ugs player list -h` to show usage.

### Changed

- **[Breaking Change]** CLI binary release assets are no longer zipped (use `chmod +x <path_to_executable>` on macos and linux to mark it as executable after downloading, or use `npm install -g ugs` to install).
- **[Breaking Change]** Replace Jint with Node.js for cloud code javascript parameter parsing. User will need to install Node.js with
  version > 14.0.0 to parse cloud code javascript.
- Updated Diagnostics to use UnityAnalyticSender instead of TelemetrySender
- Add support for `import` and `export` commands to the Lobby module.

### Fixed

- Cloud Code script with invalid parameter will fail to deploy and show in deploy result in failed catagory.
- Deploying Cloud Code C# Modules that are empty or over the size limit of 10 MB doesn't fail silently anymore.
- `ugs fetch` with `--reconcile` on empty folder with Remote Config contents now will not show `Object reference not set to an instance` error.

## [1.0.0-beta.3] - 2023-03-17

### Added

- Player Management Service commands. Run `ugs player -h` to show usage.
- Access Module Service commands. Run `ugs access -h` to show usage.
- Cloud Code C# Modules subcommands. Run `ugs cloud-code modules -h` to show usage.
- `ugs deploy` now supports Cloud Code C# modules.
- Reconcile option for Deploy command
- Added dry-run option for Deploy command
  - This allows to check what the expected outcome of a given deploy will be

### Changed

- Updated environment name validation to prevent using uppercase

- Documentation is now hosted on https://services.docs.unity.com/guides/ugs-cli/latest/general/overview
- Tweaked memory and cpu usage restrictions for Cloud Code parsing related commands

## [1.0.0-beta.2] - 2023-02-03

### Added

- Remote Config new-file command to create a default remote config file.
- Deploy Module with `fetch` command to fetch files from all services that implement `IFetchService`
  - Currently supports RemoteConfig only
- Command metrics tracking

### Changed

- Subcommands are now sorted in alphabetical order

### Fixed

- Cancelling ``ugs deploy <your-path-to-deploy>`` doesn't logs stacktraces anymore.
- `-e/--environment-name` and `-p/--project-id` options enabled for `deploy` command.
- `ugs deploy` with duplicate path will remove duplicate and deploy.
- `ugs deploy` with directory missing permission will log correct error.

## [1.0.0-beta.1] - 2023-01-16

### Added

- CLI Core Module: Configuration, Service Account Authentication Commands.
- Cloud Code Service commands
- Deploy command supporting Cloud Code and Remote Config services.
- Environment Service commands
- Lobby Service commands

### This is the first release of *UGS CLI*.
