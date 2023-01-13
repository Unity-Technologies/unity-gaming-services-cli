# How to use the CLI in popular CI/CD tools
This page provides sample recipes for using the CLI in popular CI tools.

Depending on which OS you use, installation might differ. For more information on installation, see [Installing the CLI].

**Note:** All sample recipes are based on an **Ubuntu 18.04** image.

Popular CI tools samples:

- [Unity Cloud Build](#unity-cloud-build)
- [GitHub Actions](#github-actions)
- [Jenkins](#jenkins)
- [Docker](#docker)

# Installing the CLI from CI
To install the CLI from CI, authenticate yourself with the GitHub CLI, clone the repository and use the `gh release download command`.

Download latest release: `gh release download --archive <zip|tar>`

Download specific release: `gh release download <release-version> --archive <zip|tar>`

This downloads all the binaries in zip or tar format. After unzipping the archive corresponding to your operating system, call the application from command-line.

For convenience, you may want to add the executable's path to PATH.

# Unity Cloud Build
In your [Dashboard], navigate to `DevOps` > `Cloud Build` > `Configurations` and create a new configuration.

In the advanced settings for your new configuration, you can set environment variables.

As an example, try setting the following environment variables:

| Variable                   | Value                   |
|----------------------------|-------------------------|
| UGS_CLI_PROJECT_ID         | your-project-id         |
| UGS_CLI_SERVICE_KEY_ID     | your-service-key-id     |
| UGS_CLI_SERVICE_SECRET_KEY | your-service-secret-key |

In the repository you've configured for Cloud Code, create a new `.sh` file that will contain your UGS CLI commands. Then, in your new configuration's advanced settings, set a script hook with the name of the `.sh` file. There are two fields `pre-build` which lets you execute your file before building, or `post-build` which executes your file after building.

Example content for the `.sh` file:

```sh
#!/bin/bash
ugs --version
ugs config get environment
ugs status
ugs env list
ugs deploy <directory-with-service-configurations> -j
```

# GitHub Actions
In your GitHub repository, create a `.yml` file under `.github/workflows`. You can use the following recipe in your `.yml` file as an example. For more information, see the official [GitHub Actions Documentation].

Sample Recipe:

```bash
name: UGS CLI GitHub Actions Demo

## Choose what triggers your workflow, here it is triggered for every push
on: push

## Setting environment variables
env:
  UGS_CLI_PROJECT_ID: "your-project-id"
  UGS_CLI_SERVICE_KEY_ID: "your-service-key-id"
  UGS_CLI_SERVICE_SECRET_KEY: "your-service-secret-key"

## Setting the jobs
jobs:
  demo-cli:
    ## You might want to change the runs-on depending on your needs
    runs-on: self-hosted
    steps:
    - name: Download CLI
    ## Download the UGS CLI using GitHub CLI here
    - name: Test CLI Commands
      run: |
        ugs --version
        ugs config get project-id
        ugs status
        ugs env list
        ugs deploy <directory-with-service-configurations> -j
```

# Jenkins
Follow Jenkins offical [documentation] to setup pipeline.

Example CLI recipe:

```bash
ugs --version
ugs config get project-id
ugs status
ugs env list -j
ugs deploy <directory-with-service-configurations> -j
```

To set environment variables, see the [EnvInject] Jenkins plugin.

Example environment variable configuration:

```bash
UGS_CLI_PROJECT_ID=your-project-id
UGS_CLI_SERVICE_KEY_ID=your-service-key-id
UGS_CLI_SERVICE_SECRET_KEY=your-service-secret-key
```

# Docker
First, [Download Docker], then run Docker Desktop and wait for Docker to finish initializing.

Then, create a new directory with a new file `dockerfile`.

Run `docker build --no-cache <path-to-new-directory> -f <path-to-new-dockerfile>`

CLI `dockerfile` example:

```dockerfile
# Docker image
FROM ubuntu:latest
# Setting environment variables
ENV UGS_CLI_PROJECT_ID=your-project-id
ENV UGS_CLI_SERVICE_KEY_ID=your-service-key-id
ENV UGS_CLI_SERVICE_SECRET_KEY=your-service-secret-key
# Update and get dependency packages
RUN apt-get update
RUN apt-get install sudo
RUN apt-get install gnupg -y
RUN apt-get update; apt-get install curl -y
RUN sudo apt-get install -y libicu-dev -y
# Download and install UGS CLI from GitHub CLI here
# Run test commands on the CLI
RUN ugs --version
RUN ugs config get project-id
RUN ugs status
RUN ugs env list
RUN ugs deploy <directory-with-service-configurations> -j
```
[Installing the CLI]: getting-started.md#installing-the-cli
[GitHub Actions Documentation]: https://docs.github.com/en/actions
[EnvInject]: https://plugins.jenkins.io/envinject
[Download Docker]: https://www.docker.com/products/docker-desktop/
[Dashboard]: https://dashboard.unity3d.com/
[documentation]: https://www.jenkins.io/doc/book/pipeline/getting-started/
