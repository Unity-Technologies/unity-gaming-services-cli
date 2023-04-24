# UGS CLI
For installing the UGS CLI and getting started, see [Getting Started](https://services.docs.unity.com/guides/ugs-cli/latest/general/get-started/install-the-cli).

The source code project is for reference only. You may not be able to build it due to lack of access to internal dependencies.

## Installation

### With npm
Install the CLI with npm by calling `npm install -g ugs` in your command line.

### With GitHub
Download the executable directly from the [GitHub releases](https://github.com/Unity-Technologies/unity-gaming-services-cli/releases).

On macos and linux, use `chmod +x <path_to_executable>` to mark the file as executable.

## Documentation
To see the full list of services and commands available in the UGS CLI, visit the documentation on https://services.docs.unity.com/guides/ugs-cli/latest/general/overview

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
