# Lobby module
The Lobby module is an interface to make calls to the Lobby UGS service.

Root command aliases: `lobby`

Use `ugs lobby -h` to get help on how to use the lobby commands.

Lobby Commands:

- [Bulk Update](#bulk-update)
- [Config Get](#config-get)
- [Config Update](#config-update)
- [Create](#create)
- [Delete](#delete)
- [Get-hosted](#get-hosted)
- [Get-joined](#get-joined)
- [Get](#get)
- [Heartbeat](#heartbeat)
- [Join](#join)
- [Player Update](#player-update)
- [Player Remove](#player-remove)
- [Query](#query)
- [Quickjoin](#quickjoin)
- [Reconnect](#reconnect)
- [Request-token](#request-token)
- [Update](#update)

In the following commands, where a JSON file or body is used, see the [Lobby API documentation](https://services.docs.unity.com/lobby/v1) for the format.

# Commands

## Bulk update
Bulk update a lobby.

Authentication Required: `Yes`

```bash
ugs lobby bulk-update <lobby-id> <body> [options]
```

### Arguments
* `<lobby-id>` The ID of the lobby to bulk update.
* `<body>` The JSON body containing the bulk update to apply to the lobby. If this is a file path, the content of the file is used; otherwise, the raw string is used.

### Options
|        | Alias          | Description                     |
|--------|----------------|---------------------------------|
| service-id | --service-id   | (REQUIRED) The service ID.       |
| project-id | -p, --project-id | The Unity cloud project ID. |
| environment-name | -e, --environment-name | The services environment name. |
| help       | -?, -h, --help | Display help and usage information. |
| quiet      | -q, --quiet    | Reduce logging to a minimum.     |
| json       | -j, --json     | Use JSON as the output format.   |

## Config Get
Get a lobby config from Remote Config.

Authentication Required: `Yes`

```bash
ugs lobby config get [options]
```

### Options
|        | Alias          | Description                     |
|--------|----------------|---------------------------------|
| project-id | -p, --project-id | The Unity cloud project ID. |
| environment-name | -e, --environment-name | The services environment name. |
| help        | -?, -h, --help       | Display help and usage information. |
| quiet       | -q, --quiet          | Reduce logging to a minimum.     |
| json        | -j, --json           | Use JSON as the output format.   |

## Config Update
Update an existing lobby config in Remote Config.

Authentication Required: `Yes`

```bash
ugs lobby config update [options]
```

### Arguments
* `<config-id>` The ID of the config to update.
* `<body>` The JSON body containing the updated config value. If this is a file path, the content of the file is used; otherwise, the raw string is used.

### Options
|        | Alias          | Description                     |
|--------|----------------|---------------------------------|
| project-id | -p, --project-id | The Unity cloud project ID.     |
| help       | -?, -h, --help   | Display help and usage information. |
| quiet      | -q, --quiet      | Reduce logging to a minimum.     |
| json       | -j, --json       | Use JSON as the output format.   |


### Body option
The body option must include the type and values array of a config to update. No other data should be present. For example:
```json
{
    "type": "lobby",
    "value": [
        {
            "key": "lobbyConfig",
            "type": "json",
            "schemaId": "lobby",
            "value": {
                "someBoolValue": false,
                "someIntegerValue": 30,
                "someObject": {
                    "maximum": 8,
                    "minimum": 4
                }
            }
        }
    ]
}
```

## Create
Create a new lobby.

Authentication Required: `Yes`

```bash
ugs lobby create <body> [options]
```

### Arguments
* `<body>` The lobby create body in JSON. If this is a file path, the content of the file is used; otherwise, the raw string is used.

### Options
|        | Alias          | Description                     |
|--------|----------------|---------------------------------|
| service-id | --service-id   | (REQUIRED) The service ID.       |
| player-id  | --player-id    | The player ID to impersonate.    |
| project-id | -p, --project-id | The Unity cloud project ID. |
| environment-name | -e, --environment-name | The services environment name. |
| help       | -?, -h, --help | Display help and usage information. |
| quiet      | -q, --quiet    | Reduce logging to a minimum.     |
| json       | -j, --json     | Use JSON as the output format.   |

## Delete
Delete a lobby.

Authentication Required: `Yes`

```bash
ugs lobby delete <lobby-id> [options]
```

### Arguments
* `<lobby-id>` The ID of the lobby to delete.

### Options
|        | Alias          | Description                     |
|--------|----------------|---------------------------------|
| service-id | --service-id   | (REQUIRED) The service ID.       |
| player-id  | --player-id    | The player ID to impersonate.    |
| project-id | -p, --project-id | The Unity cloud project ID. |
| environment-name | -e, --environment-name | The services environment name. |
| help       | -?, -h, --help | Display help and usage information. |
| quiet      | -q, --quiet    | Reduce logging to a minimum.     |
| json       | -j, --json     | Use JSON as the output format.   |

## Get-hosted
Get the lobbies you are currently hosting.

Authentication Required: `Yes`

```bash
ugs lobby get-hosted [options]
```

### Options
|        | Alias          | Description                     |
|--------|----------------|---------------------------------|
| service-id | --service-id   | (REQUIRED) The service ID.       |
| player-id  | --player-id    | The player ID to impersonate.    |
| project-id | -p, --project-id | The Unity cloud project ID. |
| environment-name | -e, --environment-name | The services environment name. |
| help       | -?, -h, --help | Display help and usage information. |
| quiet      | -q, --quiet    | Reduce logging to a minimum.     |
| json       | -j, --json     | Use JSON as the output format.   |

## Get-joined
Get the lobbies you are currently in.

Authentication Required: `Yes`

```bash
ugs lobby get-joined <player-id> [options]
```

### Arguments
* `<player-id>` The ID of the player being impersonated.

### Options
|        | Alias          | Description                     |
|--------|----------------|---------------------------------|
| service-id | --service-id   | (REQUIRED) The service ID.       |
| project-id | -p, --project-id | The Unity cloud project ID. |
| environment-name | -e, --environment-name | The services environment name. |
| help       | -?, -h, --help | Display help and usage information. |
| quiet      | -q, --quiet    | Reduce logging to a minimum.     |
| json       | -j, --json     | Use JSON as the output format.   |

## Get
Get a lobby.

Authentication Required: `Yes`

```bash
ugs lobby get <lobby-id> [options]
```

### Arguments
* `<lobby-id>` The ID of the lobby to get.

### Options
|        | Alias          | Description                     |
|--------|----------------|---------------------------------|
| service-id | --service-id   | (REQUIRED) The service ID.       |
| player-id  | --player-id    | The player ID to impersonate.    |
| project-id | -p, --project-id | The Unity cloud project ID. |
| environment-name | -e, --environment-name | The services environment name. |
| help       | -?, -h, --help | Display help and usage information. |
| quiet      | -q, --quiet    | Reduce logging to a minimum.     |
| json       | -j, --json     | Use JSON as the output format.   |

## Heartbeat
Heartbeat a lobby. Heartbeats renew a lobby's last accessed time to indicate to the service that the lobby hasn't been abandoned.

Authentication Required: `Yes`

```bash
ugs lobby heartbeat <lobby-id> [options]
```

### Arguments
* `<lobby-id>` The ID of the lobby to heartbeat.

### Options
|        | Alias          | Description                     |
|--------|----------------|---------------------------------|
| service-id | --service-id   | (REQUIRED) The service ID.       |
| player-id  | --player-id    | The player ID to impersonate.    |
| project-id | -p, --project-id | The Unity cloud project ID. |
| environment-name | -e, --environment-name | The services environment name. |
| help       | -?, -h, --help | Display help and usage information. |
| quiet      | -q, --quiet    | Reduce logging to a minimum.     |
| json       | -j, --json     | Use JSON as the output format.   |

## Join
Join a lobby by ID or code.

Authentication Required: `Yes`

```bash
ugs lobby join <player-details> [options]
```

### Arguments
* `<player-details>`  The JSON player details. If this is a file path, the content of the file is used; otherwise, the raw string is used.

### Options
|        | Alias          | Description                     |
|--------|----------------|---------------------------------|
| service-id | --service-id   | (REQUIRED) The service ID.       |
| lobby-id   | --lobby-id     | The ID of the lobby to join.     |
| lobby-code | --lobby-code   | The join code of the lobby to join. |
| project-id | -p, --project-id | The Unity cloud project ID. |
| environment-name | -e, --environment-name | The services environment name. |
| help       | -?, -h, --help | Display help and usage information. |
| quiet      | -q, --quiet    | Reduce logging to a minimum.     |
| json       | -j, --json     | Use JSON as the output format.   |

Either `--lobby-id` or `--lobby-code` is required, but not both.

## Player Update
Update a player in a lobby.

Authentication Required: `Yes`

```bash
ugs lobby player update <lobby-id> <player-id> <body> [options]
```

### Arguments
* `<lobby-id>`   The ID of the lobby.
* `<player-id>`  The ID of the player to update.
* `<body>`       The JSON body containing the update to apply to the player. If this is a file path, the content of the file is used; otherwise, the raw string is used.

### Options
|        | Alias          | Description                     |
|--------|----------------|---------------------------------|
| service-id | --service-id   | (REQUIRED) The service ID.       |
| project-id | -p, --project-id | The Unity cloud project ID. |
| environment-name | -e, --environment-name | The services environment name. |
| help       | -?, -h, --help | Display help and usage information. |
| quiet      | -q, --quiet    | Reduce logging to a minimum.     |
| json       | -j, --json     | Use JSON as the output format.   |

## Player Remove
Remove a player from a lobby.

Authentication Required: `Yes`

```bash
ugs lobby player remove <lobby-id> <player-id> [options]
```

### Arguments
* `<lobby-id>`   The ID of the lobby.
* `<player-id>`  The ID of the player to remove.

### Options
|        | Alias          | Description                     |
|--------|----------------|---------------------------------|
| service-id | --service-id   | (REQUIRED) The service ID.       |
| project-id | -p, --project-id | The Unity cloud project ID. |
| environment-name | -e, --environment-name | The services environment name. |
| help       | -?, -h, --help | Display help and usage information. |
| quiet      | -q, --quiet    | Reduce logging to a minimum.     |
| json       | -j, --json     | Use JSON as the output format.   |

## Query
Query for active lobbies.

Authentication Required: `Yes`

```bash
ugs lobby query [options]
```

### Options
|        | Alias          | Description                     |
|--------|----------------|---------------------------------|
| service-id | --service-id   | (REQUIRED) The service ID.           |
| player-id  | --player-id    | The ID of the player to impersonate. |
| body       | -b, --body     | The query in JSON. If this is a file path, the content of the file is used; otherwise, the raw string is used. [default: {}] |
| project-id | -p, --project-id | The Unity cloud project ID. |
| environment-name | -e, --environment-name | The services environment name. |
| help       | -?, -h, --help | Display help and usage information. |
| quiet      | -q, --quiet    | Reduce logging to a minimum.     |
| json       | -j, --json     | Use JSON as the output format.   |

## Quickjoin
QuickJoin a lobby matching a filter.

Authentication Required: `Yes`

```bash
ugs lobby quickjoin <filter> <player-details> [options]
```

### Arguments
* `<filter>` The JSON filter to use for querying. If this is a file path, the content of the file is used; otherwise, the raw string is used.
* `<player-details>` The JSON player details. If this is a file path, the content of the file is used; otherwise, the raw string is used.

### Options
|        | Alias          | Description                     |
|--------|----------------|---------------------------------|
| service-id | --service-id   | (REQUIRED) The service ID.       |
| project-id | -p, --project-id | The Unity cloud project ID. |
| environment-name | -e, --environment-name | The services environment name. |
| help       | -?, -h, --help | Display help and usage information. |
| quiet      | -q, --quiet    | Reduce logging to a minimum.     |
| json       | -j, --json     | Use JSON as the output format.   |

## Reconnect
Reconnect to a lobby.

```bash
ugs lobby reconnect <lobby-id> <player-id> [options]
```

Authentication Required: `Yes`

### Arguments
* `<lobby-id>` The ID of the lobby.
* `<player-id>` The ID of the player to impersonate.

### Options
|        | Alias          | Description                     |
|--------|----------------|---------------------------------|
| service-id | --service-id   | (REQUIRED) The service ID.       |
| project-id | -p, --project-id | The Unity cloud project ID. |
| environment-name | -e, --environment-name | The services environment name. |
| help       | -?, -h, --help | Display help and usage information. |
| quiet      | -q, --quiet    | Reduce logging to a minimum.     |
| json       | -j, --json     | Use JSON as the output format.   |

## Request-token
Request a token

Authentication Required: `Yes`

```bash
ugs lobby request-token <lobby-id> <player-id> <type> [options]
```

### Arguments
* `<lobby-id>` The ID of the lobby.
* `<player-id>` The ID of the player to impersonate.
* `<type>` The token type to request. Valid values are `VivoxJoin` or `WireJoin`.

### Options
|        | Alias          | Description                     |
|--------|----------------|---------------------------------|
| service-id | --service-id   | (REQUIRED) The service ID.       |
| project-id | -p, --project-id | The Unity cloud project ID. |
| environment-name | -e, --environment-name | The services environment name. |
| help       | -?, -h, --help | Display help and usage information. |
| quiet      | -q, --quiet    | Reduce logging to a minimum.     |
| json       | -j, --json     | Use JSON as the output format.   |

## Update
Update a lobby.

Authentication Required: `Yes`

```bash
ugs lobby update <lobby-id> <body> [options]
```

### Arguments
* `<lobby-id>` The ID of the lobby to update.
* `<body>` The JSON body containing the update to apply to the lobby. If this is a file path, the content of the file is used; otherwise, the raw string is used.

### Options
|        | Alias          | Description                     |
|--------|----------------|---------------------------------|
| service-id | --service-id   | (REQUIRED) The service ID.       |
| player-id  | --player-id    | The ID of the player to impersonate. |
| project-id | -p, --project-id | The Unity cloud project ID. |
| environment-name | -e, --environment-name | The services environment name. |
| help       | -?, -h, --help | Display help and usage information. |
| quiet      | -q, --quiet    | Reduce logging to a minimum.     |
| json       | -j, --json     | Use JSON as the output format.   |
