# Authentication Commands

The Authentication module offers service account management commands to use Unity services.

Use `ugs -h` to get help on how to use the authentication commands.

Authentication commands:

- [Login](#login)
- [Logout](#logout)
- [Status](#status)

Environment variables:

- [Logging In With Environment Variables](#logging-in-with-environment-variables)

# Commands

## Login

Safely stores the Service Account Key ID and Secret key locally.

### Interactive usage

By default, calling this command prompts you to enter your service account `key-id` and `secret-key`.

```
ugs login
Enter your key-id: <your-key-id>
Enter your secret-key: <your-secret-key>
```

### Automated usage

You can automate login by using both the `--service-key-id` option and the `--secret-key-stdin` option.<br/>
The `--service-key-id` option expects your service account `key-id` as argument.<br/>
The `--secret-key-stdin` option expects your `secret-key` in the standard input.

```
ugs login --service-key-id "your-key-id" --secret-key-stdin < "secret-key-file"
```

Where `"your-key-id"` is your service account `key-id`, and `"secret-key-file"` is the path to a file containing
your service account `secret key`.

### Options

|            | Alias              | Description                                                                    |
|------------|--------------------|--------------------------------------------------------------------------------|
| key-id     | --service-key-id   | The key-id of your service account. Must be used with "--secret-key-stdin".    |
| secret-key | --secret-key-stdin | The secret-key of your service account. Must be used with  "--service-key-id". |
| help       | -?, -h, --help     | Display help and usage information.                                            |
| quiet      | -q, --quiet        | Reduce logging to a minimum.                                                   |
| json       | -j, --json         | Use JSON as the output format.                                                 |

## Logout

Clears the Service Account Key ID and Secret Key stored locally.

```
ugs logout [options]
```

### Options

|        | Alias          | Description                         |
|--------|----------------|-------------------------------------|
| help   | -?, -h, --help | Display help and usage information. |
| quiet  | -q, --quiet    | Reduce logging to a minimum.        |
| json   | -j, --json     | Use JSON as the output format.      |

## Status

Checks if the current user has any Service Account Key ID and Secret Key stored locally.

```
ugs status [options]
```

### Options

|        | Alias          | Description                         |
|--------|----------------|-------------------------------------|
| help   | -?, -h, --help | Display help and usage information. |
| quiet  | -q, --quiet    | Reduce logging to a minimum.        |
| json   | -j, --json     | Use JSON as the output format.      |

# Environment Variables
## Logging in with Environment Variables

To log in manually use the `ugs login` command.

To log in with system environment variables, set `UGS_CLI_SERVICE_KEY_ID` and `UGS_CLI_SERVICE_SECRET_KEY` to your
system's environment variables.

Authenticated calls follow an order of priority for authentication:

`local configuration` > `Service Account Key ID and Secret key environment variables`
