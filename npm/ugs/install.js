const PACKAGE_INFO = require("./package.json");
const FS = require("fs");
const FETCH = require("node-fetch");
const { exec } = require("child_process");

const PLATFORM = process.platform;
const ARGS = process.argv.slice(2);
const VERSION = PACKAGE_INFO.version;
const RELEASE_TAG = "v" + VERSION;
const DRY_RUN = ARGS[0] === "dryrun";
const BIN_PATH = "./bin";
const USERNAME = "Unity-Technologies";
const REPO_NAME = "unity-gaming-services-cli";
const WINDOWS_X64_ASSET_NAME = "ugs-windows-x64.exe";
const MACOS_X64_ASSET_NAME = "ugs-macos-x64";
const LINUX_X64_ASSET_NAME = "ugs-linux-x64";
const API_URL = `https://api.github.com/repos/${USERNAME}/${REPO_NAME}/releases/tags/${RELEASE_TAG}`;

function exit(message) {
    console.error(message);
    process.exit(1);
}

async function sendAnalyticsAndExit(success, message) {
    if (!DRY_RUN) {
        await FETCH('https://cdp.cloud.unity3d.com/v1/events', {
            method: 'POST',
            headers: {
                'Content-Type': 'text/plain'
            },
            body: JSON.stringify({
                'type': 'ugs.cli.install_metrics.v2',
                'msg': {
                    'application_version': VERSION,
                    'operating_system': PLATFORM,
                    'installation_success': success,
                    'installation_method': 'npm install',
                    'installation_message': message
                }
            })
        })
        .catch(_ => {
            // Do nothing, analytics should not interfere with installation
        });
    }
    if (!success) {
        exit(message);
    }
}

// Determine GitHub asset name based on OS
function getAssetName() {
    switch (PLATFORM) {
        case "darwin":
            return MACOS_X64_ASSET_NAME;
            break;
        case "win32":
            return WINDOWS_X64_ASSET_NAME;
            break;
        case "linux":
            return LINUX_X64_ASSET_NAME;
            break;
        default:
            return null;
    }
}

async function downloadUnityGamingServicesCli(uri, filename) {
    if (PLATFORM === "win32") {
        filename += ".exe";
    }

    try {
        const res = await FETCH(uri);
        const dest = FS.createWriteStream(filename);
        res.body.pipe(dest);

        return new Promise((resolve, reject) => {
            dest.on('finish', resolve);
            dest.on('error', reject);
        });
    } catch (error) {
        await sendAnalyticsAndExit(false, error);
    }
}

async function execPromise(command) {
    return new Promise(function (resolve, reject) {
        exec(command, (error, stdout, stderr) => {
            if (error) {
                reject(error);
                return;
            }
            resolve();
        });
    });
}

async function chmodFile() {
    // chmod +x if on unix
    if (PLATFORM === "darwin" || PLATFORM === "linux") {
        try {
            await execPromise(`chmod +x ${BIN_PATH}/ugs`);
        } catch (error) {
            await sendAnalyticsAndExit(false, `Error executing command: ${error}`);
        }
    }
}

async function createBinFolder() {
    try {
        try {
            await FS.promises.access(BIN_PATH);
        } catch (error) {
            await FS.promises.mkdir(BIN_PATH);
        }
    } catch (error) {
        await sendAnalyticsAndExit(false, `Could not create bin folder: ${error}`);
    }
}

async function installUnityGamingServicesCli(githubAsset, assetName) {
    await createBinFolder();

    try {
        console.log(`Downloading ${assetName} from ${githubAsset.browser_download_url}`);
        await downloadUnityGamingServicesCli(githubAsset.browser_download_url, BIN_PATH + "/ugs");
    } catch (error) {
        await sendAnalyticsAndExit(false, error);
    }

    await chmodFile();
}

async function findGithubAsset(data, assetName) {
    if (!data.assets) {
        await sendAnalyticsAndExit(false, `Error fetching release data from GitHub. Please try again later or manually download the CLI from https://github.com/${USERNAME}/${REPO_NAME}/releases Full GitHub api error message: ${data.message}`);
    }

    const githubAsset = data.assets.find(asset => asset.name === assetName);

    if (!githubAsset) {
        await sendAnalyticsAndExit(false, `Asset '${assetName}' not found in release '${RELEASE_TAG}'. Try installing a different version of ugs.`);
    }

    return githubAsset;
}

FETCH(API_URL)
    .then(res => res.json())
    .then(async data => {
        if (!VERSION) {
            await sendAnalyticsAndExit(false, "Could not determine package version.");
        }

        let assetName = getAssetName();

        if (!assetName) {
            await sendAnalyticsAndExit(false, "Could not determine OS");
        }

        if (DRY_RUN) {
            await findGithubAsset(data, WINDOWS_X64_ASSET_NAME);
            await findGithubAsset(data, MACOS_X64_ASSET_NAME);
            await findGithubAsset(data, LINUX_X64_ASSET_NAME);
        } else {
            const githubAsset = await findGithubAsset(data, assetName);
            await installUnityGamingServicesCli(githubAsset, assetName);
            await sendAnalyticsAndExit(true, "");
        }
    })
    .catch(async error => await sendAnalyticsAndExit(false, `Error fetching release '${RELEASE_TAG}': ${error}`));
