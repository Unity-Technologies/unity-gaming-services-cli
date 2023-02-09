# UGS CLI
For installing the UGS CLI and getting started, see [Getting Started](docs/getting-started.md).

For information regarding services or UGS CLI commands, see [Base Modules](#base-modules) and [Service Modules](#service-modules).

The source code project is for reference only. You may not be able to build it due to lack of access to internal dependencies.

Jump to section:
- [Basic Commands](#basic-commands)
- [Base Modules](#base-modules)
- [Service Modules](#service-modules)
- [Examples](#examples)
- [System Compatibility](#system-compatibility)
- [Getting Help](#getting-help)

## Basic Commands
An UGS CLI command has the following format:
```
ugs <command> <subcommand> [<arguments>] [options]
```

To get help and list all modules:
```
ugs --help
```

To get the version of the CLI:
```
ugs --version
```

## Base Modules
[Configuration](docs/module-configuration.md)

[Environment](docs/module-environment.md)

[Authentication](docs/module-authentication.md)

[Deploy](docs/deploy-command.md)

## Service Modules
[Cloud-Code](docs/module-cloud-code.md)

[Lobby](docs/module-lobby.md)

## Samples

[Samples](Samples) are provided to demonstrate how to use CLI.

## Examples
[Example of the CLI in Unity Cloud Build](docs/ci-cd-recipes.md#unity-cloud-build)

[Example of the CLI in Github Actions](docs/ci-cd-recipes.md#github-actions)

[Example of the CLI in Jenkins](docs/ci-cd-recipes.md#jenkins)

[Example of the CLI in Docker](docs/ci-cd-recipes.md#docker)

## System Compatibility

### Windows

Requires [Windows] 10 or later.

### Linux

Requires [Ubuntu], [Alpine] or most other major distros.

### macOS

Requires [Mac OS] 10.15 or later.

## Getting Help
Please [Submit a Request](https://support.unity.com/hc/en-us/requests/new?ticket_form_id=360001936712&serviceName=cli) if you need any help.

---

[Mac OS]: https://support.apple.com/macos
[Alpine]: https://alpinelinux.org/
[Ubuntu]: https://ubuntu.com/
[Windows]: https://www.microsoft.com/windows/
