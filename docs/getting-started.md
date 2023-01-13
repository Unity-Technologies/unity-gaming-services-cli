# Getting started

Installation
- [Installing the CLI](#installing-the-cli)

Setting up common configurations:

- [Setup a Project ID](#set-up-a-project-id)
- [Setup an Environment Name](#set-up-an-environment)
- [Get Authenticated](#get-authenticated)
- [Make Service Calls](#make-service-calls)

# Installing the CLI

To install official releases of the Unity Gaming Services CLI, follow these steps.

1. Go to the [Releases] page.
2. Find the latest version.
3. Under "Assets", click and download the zip for your operating system.
4. Unzip the file.

At this point, the CLI will be usable if called by the terminal in the same directory, or by specifying the application's path.

For convenience, add the executable to PATH.

# Setting up common configurations
A few base parameters are commonly used between modules. Here's a list of some things you might want to set up:

## Set up a Project ID
Call `ugs config set project-id <your-project-id>` to set the `Project ID` to use to make service calls.

**OR**

Set `UGS_CLI_PROJECT_ID` as an environment variable in your system.

You can find your project ID in the [UGS Dashboard] at **Projects** > **Select a project** > **Project ID**.

## Set up an Environment
Call `ugs config set environment-name <your-environment-name>` to set the `Environment Name` to use to make service calls.

**OR**

Set `UGS_CLI_ENVIRONMENT_NAME` as an environment variable in your system.

You can find your environment in the [UGS Dashboard] at **Projects** > **Select a project** > **Environments** > **Select an environment** > **Name**.

## Get authenticated
### Creating a Service Account

Before you can login, you will need to create a Service Account. In the `Projects` tab, in the `Organization` section, you can create a service account. For authenticating CLI calls, we need the following:

* Your Service Account key ID
* Your Service Account secret key

Additionally, you may need to add permissions to your Service Account to allow it to modify your projects. To do this, click on your Service Account and add a project role. From there, you can choose a project and permissions.

For more information on Service Accounts, refer to the [Service Account Authentication] documentation.

### Logging in
See how to [Login Using a Command] or [Logging in With Environment Variables].

## Make Service Calls
After following the steps in the common configuration section, you should be able to make authenticated service calls. Visit the [README](/README.md) to see all the modules included in the CLI and how to use their commands, or use `ugs --help`.

[Releases]: https://github.com/Unity-Technologies/unity-gaming-services-cli/releases
[UGS Dashboard]: https://dashboard.unity3d.com/
[Service Account Authentication]: https://services.docs.unity.com/docs/service-account-auth
[Login Using a Command]: /docs/module-authentication.md#login
[Logging in With Environment Variables]: module-authentication.md#logging-in-with-environment-variables
