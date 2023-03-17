# Changelog

All notable changes to UGS CLI will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

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
