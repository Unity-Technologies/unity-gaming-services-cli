# Environment module
The Environment module offers commands to access or modify UGS environments.

Root command aliases: `env`

Use `ugs env -h` to get help on how to use the environment commands.

Environment commands

- [Add](#add)
- [Delete](#delete)
- [List](#list)
- [Use](#use)

# Commands
## Add
Add a new environment to a UGS project.

Authentication Required: `Yes`

```bash
ugs env add <environment-name> [options]
```

### Arguments
`<environment-name>` The services environment name

### Options
|        | Alias          | Description                     |
|--------|----------------|---------------------------------|
| project-id | -p, --project-id | The Unity cloud project ID. |
| help   | -?, -h, --help | Display help and usage information. |
| quiet  | -q, --quiet    | Reduce logging to a minimum.     |
| json   | -j, --json     | Use JSON as the output format.   |

## Delete
Delete an environment from a UGS project.

Authentication Required: `Yes`

```bash
ugs env delete <environment-name> [options]
```

### Arguments
`<environment-name>` The services environment name

### Options
|        | Alias          | Description                     |
|--------|----------------|---------------------------------|
| project-id | -p, --project-id | The Unity cloud project ID. |
| help   | -?, -h, --help | Display help and usage information. |
| quiet  | -q, --quiet    | Reduce logging to a minimum.     |
| json   | -j, --json     | Use JSON as the output format.   |

## List
List environments from a UGS project.

Authentication Required: `Yes`

```bash
ugs env list [options]
```

### Options
|        | Alias          | Description                     |
|--------|----------------|---------------------------------|
| project-id | -p, --project-id | The Unity cloud project ID. |
| help   | -?, -h, --help | Display help and usage information. |
| quiet  | -q, --quiet    | Reduce logging to a minimum.     |
| json   | -j, --json     | Use JSON as the output format.   |

## Use
Sets the environment to use in project configuration. This is a utility command that does the exact same thing as `ugs config set environment`.

Authentication Required: `No`

```bash
ugs env use <environment-name> [options]
```

### Arguments
`<environment-name>` The services environment name

### Options
|        | Alias          | Description                     |
|--------|----------------|---------------------------------|
| help   | -?, -h, --help | Display help and usage information. |
| quiet  | -q, --quiet    | Reduce logging to a minimum.     |
| json   | -j, --json     | Use JSON as the output format.   |
