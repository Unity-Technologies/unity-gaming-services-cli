# Project roles

Project roles grant access to project-level data, which includes APIs that only apply to individual projects you choose. So to use some of the UGS CLI commands, you need to have the correct project roles linked to your project ID and Service Account. 
You can link the project roles to your Service Account and project ID under the `Services Accounts` section in the Unity Dashboard. For more information, see [Creating a Service Account](https://services.docs.unity.com/docs/service-account-auth).
The tables below shows the project roles required to execute commands for each module.

## Environment module

| Project role | Description                 |
|--------------|------------------------------|
| `Unity Environments Admin`       | Grants full access to all environments in a project.   |

## Cloud Code module

| Project role | Description                 |
|--------------|------------------------------|
| `Unity Environments Admin`       | Grants full access to all environments in a project.   |
| `Cloud Code Script Editor`       | Grants permissions necessary for editing cloud code scripts.   |
| `Cloud Code Script Publisher`       | Grants permissions necessary for publishing cloud code scripts.   |
| `Cloud Code Script Viewer`       | Grants permissions necessary for viewing cloud code scripts.   |

## Economy module

| Project role | Description                 |
|--------------|------------------------------|
| `Unity Environments Admin`      | Grants full access to all environments in a project.   |
| `Economy Resource Editor`       | Grants permissions necessary for creating, editing and deleting economy resources.   |
| `Economy Resource Publisher`      | Grants permissions necessary for publishing economy configurations.   |
| `Economy Resource Viewer`       | Grants permissions necessary for listing economy resources.   |

## Lobby module

| Project role | Description                 |
|--------------|------------------------------|
| `Unity Environments Admin`      | Grants full access to all environments in a project.   |
| `Remote Config Admin`       | Grants access to the Remote Config admin API.   |

## Deploy Command
Currently Cloud Code and Remote Config services support deploy command. To deploy for all services you need the following roles:

| Project role | Description                 |
|--------------|------------------------------|
| `Unity Environments Admin`      | Grants full access to all environments in a project.   |
| `Remote Config Admin`       | Grants access to the Remote Config admin API.   |
| `Cloud Code Script Editor`       | Grants permissions necessary for editing cloud code scripts.   |
| `Cloud Code Script Publisher`       | Grants permissions necessary for publishing cloud code scripts.   |
| `Cloud Code Script Viewer`       | Grants permissions necessary for viewing cloud code scripts.   |
