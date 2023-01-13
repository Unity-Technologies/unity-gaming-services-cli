# Configuration keys (Keep up to date)
The table below describes the keys that can be `set` or `get` from local configuration, and the environment variable keys.

| ConfigKey          | EnvironmentKey               | Meaning                                                                          |
|--------------------| ---------------------------- | -------------------------------------------------------------------------------- |
| `project-id`       | `UGS_CLI_PROJECT_ID`         | The ID of the cloud project.                                                     |
| `environment-name` | `UGS_CLI_ENVIRONMENT_NAME`   | Current config environment name for the project.                                 |
| n/a                | `UGS_CLI_SERVICE_KEY_ID`     | Service key ID for service account authentication (requires service secret key). |
| n/a                | `UGS_CLI_SERVICE_SECRET_KEY` | Service secret key for service account authentication (requires service key ID). |
| n/a                | `UGS_CLI_TELEMETRY_DISABLED` | Disables sending telemetry when set.                                             |
