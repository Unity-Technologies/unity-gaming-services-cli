# Configuration module
Update configuration by setting or getting keys from local configuration.

Root command aliases: `config`

Use `ugs config -h` to get help on how to use the configuration commands.

See [configuration keys].

Configuration commands

- [Get](#get)
- [Set](#set)
- [Delete](#delete)

# Commands
## Get
Get the value of a configuration for the given key.

Authentication Required: `No`

```
ugs config get <key> [options]
```

### Arguments
`<key>` The key to get from local [configuration keys] <br>

### Options
|        | Alias          | Description                     |
|--------|----------------|---------------------------------|
| help   | -?, -h, --help | Display help and usage information. |
| quiet  | -q, --quiet    | Reduce logging to a minimum.    |
| json   | -j, --json     | Use JSON as the output format.   |

## Set
Update configuration with a value for the given key.

Authentication Required: `No`

```
ugs config set <key> <value> [options]
```

### Arguments
`<key>` The key to get from local [configuration keys] <br>
`<value>` The value to assign to the key

### Options
|        | Alias          | Description                     |
|--------|----------------|---------------------------------|
| help   | -?, -h, --help | Display help and usage information. |
| quiet  | -q, --quiet    | Reduce logging to a minimum.     |
| json   | -j, --json     | Use JSON as the output format.   |

## Delete
Delete the value of a key in configuration.

Authentication Required: `No`

```
ugs config delete [options]
```

You must call this command with the `--key <key>` option or with the `--all` option.

See [configuration keys] for the list of keys.

### Options
|        | Alias           | Description                     |
|--------|-----------------|---------------------------------|
| key    | -k, --key <key> | A key in configuration.          |
| all    | -a, --all       | All keys in configuration.       |
| force  | -f, --force     | Force an operation.              |
| help   | -?, -h, --help  | Display help and usage information. |
| quiet  | -q, --quiet     | Reduce logging to a minimum.    |
| json   | -j, --json      | Use JSON as the output format.   |

[configuration keys]: configuration-keys.md#configuration-keys-list-keep-up-to-date
