#!/usr/bin/env node
const FS = require('fs');
const { spawn } = require('child_process')
const PLATFORM = process.platform;
const ARGS = process.argv.slice(2);
const WIN_PATH = "/bin/ugs.exe";
const UNIX_PATH = "/bin/ugs";
let binPath = "";

const OS_PATH_ERROR = "Error: Could not determine the path for the ugs executable for your OS. Maybe your OS isn't supported yet? Currently supported: Linux, macOS, Windows.";
const BIN_PATH_ERROR = `Error: Could not determine the path for the ugs executable. This is likely due to a problem while installing the package with npm. Check that the unzipped executable is in the ${__dirname}/bin folder. If it is there, you might need to modify the file's permissions or use chmod +x on it. If it is not there, try reinstalling the package with npm. If nothing works, try downloading the executable via GitHub, then put it in the bin folder and file a support ticket.`;

// Determine the path of the executable
switch (PLATFORM) {
    case "darwin":
        binPath = UNIX_PATH;
        break;
    case "win32":
        binPath = WIN_PATH;
        break;
    case "linux":
        binPath = UNIX_PATH;
        break;
    default:
        console.error(OS_PATH_ERROR);
        process.exit(1);
}

const UGS_COMMAND = __dirname + binPath;

// Verify executable exists
FS.access(UGS_COMMAND, FS.constants.F_OK, (error) => {
    if (error) {
        console.error(BIN_PATH_ERROR);
        process.exit(1);
    }
});

try {
    // Call the command
    const shell = spawn(UGS_COMMAND, ARGS, { stdio: "inherit" })

    shell.on('error', (error) => {
        console.log("npm package error: " + error);
        process.exit(1);
    });

    // Set exit code
    shell.on('exit', (code) => {
        process.exit(code);
    });
} catch (error) {
    console.log("npm package error: " + error);
    process.exit(1);
}
