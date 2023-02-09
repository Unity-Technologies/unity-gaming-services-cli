# Changelog

All notable changes to UGS CLI will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

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
