#!/usr/bin/env bash

# Author: Unity Technologies
#
#---------------------------------------------------------------
# Purpose
#---------------------------------------------------------------
#
# This shell installer allows you to install and uninstall the
# UGS CLI.
#
# The unity gaming services (UGS) CLI is a unified command line
# interface tool for gaming services.
#
#---------------------------------------------------------------
# Usage
#---------------------------------------------------------------
#
# To install the UGS CLI using this bash script, first get the current installer version
# and shasum hash from https://services.docs.unity.com/guides/ugs-cli/latest/general/get-started/install-the-cli/
#
# Then replace <INSTALLER_VERSION> and <SHASUM_HASH> by their respective values and call this command:
#
#     curl -sLo ugs_installer ugscli.unity.com/<INSTALLER_VERSION> && shasum -c <<<"<SHASUM_HASH>  ugs_installer" && bash ugs_installer
#
# from your command line.
#
# To install a specific version of the UGS CLI, you can add a
# version parameter like so:
#
#     curl -sLo ugs_installer ugscli.unity.com/<INSTALLER_VERSION> && shasum -c <<<"<SHASUM_HASH>  ugs_installer" && version=<version> bash ugs_installer
#
#     example: curl -sLo ugs_installer ugscli.unity.com/<INSTALLER_VERSION> && shasum -c <<<"<SHASUM_HASH>  ugs_installer" && version=1.0.0-beta.4 bash ugs_installer
#
# To uninstall the UGS CLI:
#
#     curl -sLo ugs_installer ugscli.unity.com/<INSTALLER_VERSION> && shasum -c <<<"<SHASUM_HASH>  ugs_installer" && uninstall=true bash ugs_installer
#
#---------------------------------------------------------------
# Options
#---------------------------------------------------------------
#
# version=<version>
# description: When added, it allows you to specify the version
#              of the UGS CLI that you want to install
# default:     latest
# example:     version=1.0.0-beta.4
#
# uninstall=<bool>
# description: When set to true, it allows you to uninstall the UGS CLI
# default:     false
# example:     uninstall=true
#
# diagnostics=<bool>
# description: Set to false to deactivate the sending of
#              installation diagnostics
# default:     true
# example:     diagnostics=false
#
#---------------------------------------------------------------
# Useful Links
#---------------------------------------------------------------
#
# Documentation:
# https://services.docs.unity.com/guides/ugs-cli/latest/general/overview
#
# GitHub repo:
# https://github.com/Unity-Technologies/unity-gaming-services-cli
#
# License:
# https://github.com/Unity-Technologies/unity-gaming-services-cli/blob/main/License.md
#
# Support:
# https://support.unity.com/hc/en-us/requests/new?ticket_form_id=360001936712&serviceName=cli
#

# GitHub repository metadata
ORGANIZATION="Unity-Technologies"
REPO_NAME="unity-gaming-services-cli"

# Command line echo utilities
INFORMATION_TAG="[Information]"
WARNING_TAG="\033[33m[Warning]\033[0m"
ERROR_TAG="\033[31m[Error]\033[0m"
SUCCESS_TAG="\033[32m[Success]\033[0m"
TAB=">   "

INSTALL_DIRECTORY="/usr/local/bin"
SUPPORT_URL="https://support.unity.com/hc/en-us/requests/new?ticket_form_id=360001936712&serviceName=cli"
UGS_EXISTS=$(which ugs)

if [[ $uninstall == "true" ]]
then
    INSTALLATION_METHOD="uninstall"
else
    INSTALLATION_METHOD="install"
fi

# Exit when a command returns non-zero exit code
set -e

send_installation_metrics () {
if [[ "$telemetry" != false ]]
then
    curl -s --location --request POST 'https://cdp.cloud.unity3d.com/v1/events' \
    --header 'Content-Type: text/plain' \
    --data-raw "{
    \"common\": {
        \"uuid\": \"\"
    }
}
{
    \"type\": \"ugs.cli.install_metrics.v2\",
    \"msg\": {
        \"application_version\": \"$version\",
        \"operating_system\": \"$OPERATING_SYSTEM\",
        \"installation_success\": $1,
        \"installation_method\": \"bash $INSTALLATION_METHOD\",
        \"installation_message\": \"$2\"
    }
}"
fi
}

cleanup() {
    echo -e "$ERROR_TAG Fatal Error."
    echo -e "$TAB Try again with elevated privileges, or try later."
    echo -e "$TAB If your problem does not get resolved, open a ticket at $SUPPORT_URL"
    send_installation_metrics false "bash $INSTALLATION_METHOD" "Fatal Error: $?"
}

# Upon error, we run cleanup()
trap cleanup ERR

# This section manages the uninstallation of the CLI when a user specifies
# uninstall=true in the command line. If all checks pass, we proceed to uninstall the CLI.
if [[ ! -z $UGS_EXISTS ]]
then
    NPM_UGS_EXISTS=$(npm list -g ugs > /dev/null 2>&1; echo $?)

    if [[ $INSTALLATION_METHOD == "uninstall" ]]
    then
        if [[ $NPM_UGS_EXISTS == 0 ]]
        then
            echo -e "$ERROR_TAG Cannot uninstall the UGS CLI."
            echo -e "$TAB Your version of UGS was installed with npm."
            echo -e "$TAB Try uninstalling it using 'npm uninstall -g ugs'."
            send_installation_metrics false "Must uninstall with npm"
            exit 1
        else
            echo -e "$INFORMATION_TAG Starting uninstall"
            echo -e "$TAB Removing binaries..."
            sudo rm $UGS_EXISTS
            echo -e "$TAB Binaries removed."
            echo ""
            echo -e "$SUCCESS_TAG ugs uninstalled"
            send_installation_metrics true "success"
            exit 0
        fi
    fi
elif [[ -z $UGS_EXISTS && $INSTALLATION_METHOD == "uninstall" ]]
then
    echo -e "$ERROR_TAG Cannot uninstall the UGS CLI."
    echo -e "$TAB Could not find ugs on your system."
    send_installation_metrics false "UGS CLI not found"
    exit 1
fi

# Small check to see if it's possible to install the binaries. Will prompt an error if ugs already exists.
if [[ ! -z $UGS_EXISTS ]]
then
    if [[ $NPM_UGS_EXISTS == 0 ]]
    then
        echo -e "$ERROR_TAG Cannot install the UGS CLI."
        echo -e "$TAB A version of the UGS CLI already exist and was installed with npm."
        echo -e "$TAB Try uninstalling it using 'npm uninstall -g ugs'."
        send_installation_metrics false "Already installed with npm"
        exit 1
    else
        echo -e "$ERROR_TAG Cannot install the UGS CLI."
        echo -e "$TAB The UGS CLI already exists at '$UGS_EXISTS'"
        echo -e "$TAB Remove that version and try again."
        send_installation_metrics false "Another version of the CLI already exists"
        exit 1
    fi
fi

echo -e "$INFORMATION_TAG Installing the UGS CLI"
echo -e "$TAB Verifying system compatibility..."

# Get operating system type
OPERATING_SYSTEM=$(uname -s | tr '[:upper:]' '[:lower:]')

# Rename darwin to macos for clarity
if [[ $OPERATING_SYSTEM == "darwin" ]]
then
    OPERATING_SYSTEM="macos"
fi

# Verify operating system support
if [[ $OPERATING_SYSTEM != "macos" && $OPERATING_SYSTEM != "linux" ]]
then
    echo ""
    echo -e "$ERROR_TAG Your operating system '$OPERATING_SYSTEM' is not supported."
    echo -e "$TAB Currently supported operating systems for this bash installer are Linux and MacOS"
    echo -e "$TAB If your operating system is Linux or MacOS, open a ticket here:"
    echo -e "$TAB $SUPPORT_URL"
    send_installation_metrics false "Operating System not supported"
    exit 1
fi

ASSET_NAME="ugs-$OPERATING_SYSTEM-x64"

# Determine the asset download url
if [[ ! -z $version ]]
then
    RELEASE_TAG="v$version"

    if [[ $RELEASE_TAG =~ v1\.0\.0-beta\.[123] ]]
    then
        echo ""
        echo -e "$ERROR_TAG UGS CLI version $RELEASE_TAG does not support bash installer."
        echo -e "$TAB Try installing a more recent version of the CLI."
        send_installation_metrics false "Unsupported CLI version"
        exit 1
    fi

    response=$(curl -s "https://api.github.com/repos/$ORGANIZATION/$REPO_NAME/releases/tags/$RELEASE_TAG")

    if [[ "$response" != *"tag_name"* ]]
    then
        echo ""
        echo -e "$ERROR_TAG Release version $version does not exist."
        echo -e "$TAB The release version specified does not exist."
        echo -e "$TAB To see a list of all the released versions of the UGS CLI, visit:"
        echo -e "$TAB https://github.com/Unity-Technologies/unity-gaming-services-cli/releases"
        send_installation_metrics false "Version specified does not exist"
        exit 1
    fi

    GITHUB_API_URL="https://github.com/$ORGANIZATION/$REPO_NAME/releases/download/$RELEASE_TAG/$ASSET_NAME"
else
    GITHUB_API_URL="https://github.com/$ORGANIZATION/$REPO_NAME/releases/latest/download/$ASSET_NAME"
fi

# If we reach this point, all checks have passed. We have all the information to download
# and install the UGS CLI.
#
# Download UGS CLI to /usr/local/bin
echo -e "$TAB Downloading binaries to $INSTALL_DIRECTORY..."
sudo mkdir -p "$INSTALL_DIRECTORY"
sudo curl -o "$INSTALL_DIRECTORY/ugs" -L --progress-bar $GITHUB_API_URL

# Use chmod +x on the binaries to mark them as executable
echo -e "$TAB Marking binaries as executable..."
sudo chmod +x "$INSTALL_DIRECTORY/ugs"

# We're done, nice! All that's left is a check to see if the executable is in PATH
echo -e "$TAB All done."
echo ""
echo -e "$SUCCESS_TAG Installation completed"

UGS_VERSION=$(ugs --version > /dev/null 2>&1; echo $?)

# Check if the executable is in PATH
if [[ ! ":$PATH:" == *":$INSTALL_DIRECTORY:"* ]];
then
    echo -e "$WARNING_TAG UGS CLI was installed correctly, but could not be automatically added to your PATH."
    echo -e "$TAB To be able to call ugs, add $INSTALL_DIRECTORY to your PATH by modifying ~/.profile or ~/.bash_profile, then reopen your command line."
    send_installation_metrics true "Success, binaries not in PATH"
else
    echo -e "$TAB To get started, type 'ugs -h'."
    send_installation_metrics true "Success"
fi
