# Unauthorized Error 403

This error can be encountered for many different reasons, but usually is caused by a lack of permissions.

Here's what you should look for when you get this error:

## Check that your service account is valid

One common factor that might trigger this error is logging in with an invalid or wrong service account.

First, make sure that you have logged in with the right service account Key ID and Secret key. Then, make sure that the service account you've logged in with has the required project roles for the project that you are targeting.

## Check that you have the correct project roles

Another common reason why you might encounter this error is that your service account may not have the necessary project roles.

For example, to delete an environment with the `ugs env delete` command, the Unity Environments Admin role must be added to your service account for the project you're trying to delete an environment from.

To see the list of project roles and to learn more about how to add them to your service account, see [project roles].

## Check that you have the correct project-id and environment name

After verifying you are logged in with the right service account Key ID and Secret key and also making sure that you have the correct project roles, check that you are targeting the right project and environment.

The service account usually has project roles for specific projects, so it's important to target the right project to be able to make authenticated service calls.

To set your project id, use `ugs config set project-id <project-id>`.

To set your environment name, use `ugs config set environment-name <environment-name>`.

[project roles]: /docs/project-roles.md
