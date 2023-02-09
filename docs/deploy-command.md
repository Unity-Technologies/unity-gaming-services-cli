# Deploy Command

The Deploy command is an interface to deploy configuration files of supported services to the backend.<br/>
Supported services: [remote-config], [cloud-code].

You can follow the [instructions] to deploy sample contents.

## Command

```
ugs deploy <paths> [options]
```

### Arguments

`<paths>` The paths to deploy from. Accepts multiple directories or file paths. Specify '.' to deploy the current
directory.

### Options

|             | Alias                  | Description                             		 |
| ----------- | ---------------------- | --------------------------------------------------------|
| project-id  | -p, --project-id       | The Unity cloud project ID.             		 |
| environment | -e, --environment-name | The services environment name.          		 |
| help        | -?, -h, --help         | Display help and usage information.     		 |
| quiet       | -q, --quiet            | Reduce logging to a minimum.                            |
| json        | -j, --json             | Use JSON as the output format.                          |
| reconcile   | --reconcile            | Delete content not part of deploy                       |
| dry run     | --dry-run              | Perform a trial run with no changes made  |



---
[remote-config]: module-remote-config.md
[cloud-code]: module-cloud-code.md
[instructions]: /Samples/Deploy/instructions.md
