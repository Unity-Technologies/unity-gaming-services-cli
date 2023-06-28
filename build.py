#!/usr/bin/env python3
import sys
import os
import argparse
import subprocess
import stat
import xml.etree.ElementTree as ET

PROJECT_PATH = "Unity.Services.Cli/Unity.Services.Cli/Unity.Services.Cli.csproj"
BUILD_DIR = "build"


PLATFORMS = [
    { "name": "Windows"             , "folder": "windows"    , "executable_name": "ugs.exe" , "arch": "win-x64"        , "skip_env_var": "SKIP_WINDOWS_BUILD"    },
    { "name": "MacOS"               , "folder": "macos"      , "executable_name": "ugs"     , "arch": "osx-x64"        , "skip_env_var": "SKIP_MACOS_BUILD"      },
    { "name": "Linux"               , "folder": "linux"      , "executable_name": "ugs"     , "arch": "linux-x64"      , "skip_env_var": "SKIP_LINUX_BUILD"      },
    { "name": "Linux-Musl (Alpine)" , "folder": "linux-musl" , "executable_name": "ugs"     , "arch": "linux-musl-x64" , "skip_env_var": "SKIP_LINUX_MUSL_BUILD" },
]

def main():
    if not os.path.exists(BUILD_DIR):
        os.makedirs(BUILD_DIR)

    platforms_to_build = [platform for platform in PLATFORMS if platform.get("skip_env_var") not in os.environ]

    create_version_file()

    log_build_flow(platforms_to_build)

    args = parse_input()
    extra_defines = get_extra_defines(args)
    base_build_command = "dotnet publish " + PROJECT_PATH + " --self-contained true --nologo" \
                         + " -p:PublishSingleFile=true -p:TrimUnusedDependencies=true -p:DebugType=None -c Release"
    if len(extra_defines) > 0:
        base_build_command = base_build_command + " -p:ExtraDefineConstants=" + extra_defines

    for platform in platforms_to_build:
        build_for_platform(base_build_command, platform)


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


def log_build_flow(platforms_to_build):
    for platform in platforms_to_build:
        print("Building " + platform["executable_name"] + " for " + platform["name"] + " in " + BUILD_DIR)


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


def build_for_platform(base_build_command, platform):
    print("Building " + platform["name"])
    build_command = base_build_command + " -r " + platform["arch"] + " -o " + BUILD_DIR + "/" + platform["folder"] + " /flp:--verbosity:diag /flp:logfile=build/build.log"
    build_process_output = subprocess.run(build_command, shell=True, check=True)
    print(build_process_output)
    executable_path = os.path.join(BUILD_DIR, platform["folder"], platform["executable_name"])
    executable_stat = os.stat(executable_path)
    os.chmod(executable_path, executable_stat.st_mode | stat.S_IEXEC)


if __name__ == "__main__":
    sys.exit(main())
