#!/usr/bin/env python3
import sys
import os
import argparse
import subprocess
import stat
import xml.etree.ElementTree as ET

EXECUTABLE_NAME = "ugs"
PROJECT_PATH = "Unity.Services.Cli/Unity.Services.Cli/Unity.Services.Cli.csproj"
BUILD_DIR = "build"


def main():
    if not os.path.exists(BUILD_DIR):
        os.makedirs(BUILD_DIR)

    create_version_file()

    skip_windows_build = "SKIP_WINDOWS_BUILD" in os.environ
    skip_macos_build = "SKIP_MACOS_BUILD" in os.environ
    skip_linux_build = "SKIP_LINUX_BUILD" in os.environ
    skip_alpine_linux_build = "SKIP_ALPINE_LINUX_BUILD" in os.environ

    log_build_flow(skip_windows_build, skip_macos_build, skip_linux_build, skip_alpine_linux_build)

    args = parse_input()
    extra_defines = get_extra_defines(args)
    base_build_command = "dotnet publish " + PROJECT_PATH + " --self-contained true --nologo" \
                         + " -p:PublishSingleFile=true -p:TrimUnusedDependencies=true -p:DebugType=None -c Release"
    if len(extra_defines) > 0:
        base_build_command = base_build_command + " -p:ExtraDefineConstants=" + extra_defines

    build_for_platform(base_build_command, "win-x64", "windows", skip_windows_build, ".exe")
    build_for_platform(base_build_command, "osx-x64", "macos", skip_macos_build)
    build_for_platform(base_build_command, "linux-x64", "linux", skip_linux_build)
    build_for_platform(base_build_command, "linux-musl-x64", "linux-alpine", skip_alpine_linux_build)


def create_version_file():
    if "YAMATO_JOB_ID" not in os.environ:
        return

    tree = ET.parse(PROJECT_PATH)
    root = tree.getroot()
    cli_version = ""
    for prefix_tag in root.findall("PropertyGroup/VersionPrefix"):
        cli_version = prefix_tag.text

    for suffix_tag in root.findall("PropertyGroup/VersionSuffix"):
        if suffix_tag.text != None:
            cli_version = cli_version + "-" + suffix_tag.text

    print("Version: " + cli_version)
    version_file_path = os.path.join(BUILD_DIR, "version.txt")
    with open(version_file_path, "w") as version_file:
        version_file.write(cli_version)


def log_build_flow(skip_windows_build, skip_macos_build, skip_linux_build, skip_alpine_linux_build):
    included_platforms = ""
    if not skip_windows_build:
        included_platforms = included_platforms + " Windows"
    if not skip_macos_build:
        included_platforms = included_platforms + " MacOS"
    if not skip_linux_build:
        included_platforms = included_platforms + " Linux"
    if not skip_alpine_linux_build:
        included_platforms = included_platforms + " Linux(Alpine)"
    print("Building " + EXECUTABLE_NAME + " for " + included_platforms + " in " + BUILD_DIR)


def parse_input():
    parser = argparse.ArgumentParser()
    parser.add_argument("--extra-defines", help="The custom defines you want to pass to the build process.", type=str,
                        required=False, default="")
    args = parser.parse_args()
    return args


def get_extra_defines(args):
    extra_defines = args.extra_defines
    if "EXTRA_CLI_DEFINES" in os.environ:
        if len(extra_defines) > 0:
            extra_defines = extra_defines + ";" + os.environ["EXTRA_CLI_DEFINES"]
        else:
            extra_defines = os.environ["EXTRA_CLI_DEFINES"]

    return extra_defines


def build_for_platform(base_build_command, platform_build_argument, platform_name, skip_build,
                       executable_extension=""):
    if skip_build:
        print("Skipping " + platform_name + " build")
        return

    print("Building " + platform_name)
    build_command = base_build_command + " -r " + platform_build_argument + " -o " + BUILD_DIR + "/" + platform_name + " /flp:--verbosity:diag /flp:logfile=build/build.log"
    build_process_output = subprocess.run(build_command, shell=True, check=True)
    print(build_process_output)
    executable_path = os.path.join(BUILD_DIR, platform_name, EXECUTABLE_NAME + executable_extension)
    executable_stat = os.stat(executable_path)
    os.chmod(executable_path, executable_stat.st_mode | stat.S_IEXEC)


if __name__ == "__main__":
    sys.exit(main())
