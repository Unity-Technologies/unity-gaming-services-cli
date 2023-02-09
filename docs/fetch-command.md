# Fetch Command

The Deploy command is an interface to Fetch configurations of supported services from the backend.<br/>
Supported services: [remote-config].

## Command

```
ugs fetch <path> [options]
```

### Arguments

`<path>` the path to fetch to. Accepts a single directory. Specify '.' to fetch into the current
directory.

### Options

|             | Alias                  | Description                             		         |
| ----------- | ---------------------- |---------------------------------------------------------|
| project-id  | -p, --project-id       | The Unity cloud project ID.             		         |
| environment | -e, --environment-name | The services environment name.          		         |
| help        | -?, -h, --help         | Display help and usage information.     		         |
| quiet       | -q, --quiet            | Reduce logging to a minimum.                            |
| json        | -j, --json             | Use JSON as the output format.                          |
| reconcile   | --reconcile            | Content that is not updated will be created at the root |
| dry run     | --dry-run              | Perform a trial run with no changes made   |

[remote-config]: module-remote-config.md

[cloud-code]: module-cloud-code.md
