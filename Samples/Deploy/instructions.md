# Deploy Sample Contents

1. Please check [`deploy`] command to learn how to use deploy commands.
2. Ensure your [Service Account] have the project roles for [Deploy Command].
3. [Login] CLI with service account.
4. Set [configuration] for your project-id and environment.
5. Now follow the instructions below to deploy for different services.

[Deploy Cloud Code Script](#deploy-cloud-code-script)<br>
[Deploy Remote Config](#deploy-remote-config)<br>
[Deploy Economy](#deploy-economy)<br>
[Deploy Leaderboards](#deploy-leaderboards)<br>
[Deploy Access](#deploy-access)<br>

## Deploy Cloud Code Script

Run command from [Samples/Deploy] directory:
```
ugs deploy ./CloudCode/Script
```
You will find [Script.js] published in your dashboard for the configured project and environment. The command deploys supported contents in `CloudCode/Script` directory. 

### Create Cloud Code Script

To create a deployable cloud code script, you need `module.exports` and `module.exports.params` in your script:

```JS
module.exports = async ({ params, context, logger }) => {
  // Please define your script logic
}

// Please define your script parameters
module.exports.params = {
    range: "NUMERIC"
};
```
Please take [Script.js] as an example. For more details, please check [Declare parameters in the script].

## Deploy Cloud Code Module

Run command from [Samples/Deploy] directory:
```
ugs deploy ./CloudCode/Module
```
You will find [Module.ccm] published in your dashboard for the configured project and environment. The command deploys supported contents in `CloudCode/Module` directory. 

### Create Cloud Code Module

To create a deployable cloud code module, you need either a `.ccm` file or a `.sln`. When deploying a `.sln` file the entry point project will be determined by the publish profile.

Please take [Module.ccm] as an example. For more details, please check [Create a Module Project] or [Create a Module Project using the CLI].

## Deploy Remote Config

Run command from [Samples/Deploy] directory:
```
ugs deploy ./RemoteConfig
```
You will find all the keys in [configuration.rc] published in your dashboard for the configured project and environment. The command deploys support contents in `RemoteConfig` directory.

### Create Remote Config Files:

To create a deployable remote config file, you need a `.rc` file with the following pattern:
```Json
{
  "$schema": "https://ugs-config-schemas.unity3d.com/v1/remote-config.schema.json",
  "entries": {
    "your-unique-config-key": 100.2
  },
  "types" : {
    "your-unique-config-key": "FLOAT"
  }
}
```
Please take [configuration.rc] as an example. For more details, please check [Remote Config files].

## Deploy Economy

Run command from [Samples/Deploy] directory:
```
ugs deploy ./Economy
```

You will find the resource from [CURRENCY.ecc], [INVENTORY_ITEM.eci], [VIRTUAL_PURCHASE.ecv] and [REAL_MONEY_PURCHASE.ecr] published in your dashboard for the configured project and environment.

### Create Economy Files
There are 4 file formats for Economy:
- `.ecc` for Currency
- `.eci` for Inventory Item
- `.ecr` for Real Money Purchase
- `.ecv` for Virtual Purchase

All of the files, regardless of type, should contain a json containing the required information for the specific Economy resource. You can find out what information to put in each file by looking at the [Economy resource schemas].

Some fields may be omitted from the resource file, such as `type` (inferred by file extension), `id` (defaults to be equal to name), `customData` and other optional fields.

### File Content Examples
File: GOLD.ecc
```Json
{
  "name": "GOLD",
  "initial": 10,
  "max": 1000
}
```

Check out examples for [CURRENCY.ecc], [INVENTORY_ITEM.eci], [VIRTUAL_PURCHASE.ecv] and [REAL_MONEY_PURCHASE.ecr].

## Deploy Leaderboards

Run command from [Samples/Deploy] directory:
```
ugs deploy ./Leaderboards
```
You will find the resource from [lbsample.lb] published in your dashboard for the configured project and environment.

### Create Leaderboards Files:

To create a deployable leaderboards file, you need a `.lb` file with the following pattern:
```Json
{
  "$schema": "https://ugs-config-schemas.unity3d.com/v1/leaderboards.schema.json",
  "SortOrder": "asc",
  "UpdateType": "keepBest",
  "Name": "My Leaderboard",
  "ResetConfig": {
    "Start": "2023-08-25T00:00:00-04:00",
    "Schedule": "0 12 1 * *"
  },
  "TieringConfig": {
    "Strategy": "score",
    "Tiers": [
      {
        "Id": "Gold",
        "Cutoff": 200.0
      },
      {
        "Id": "Silver",
        "Cutoff": 100.0
      },
      {
        "Id": "Bronze"
      }
    ]
  }
}
```
Please take [lbsample.lb] as an example. For more details, please check [Leaderboards API] and/or [Leaderboards schema].

## Deploy Access

Run command from [Samples/Deploy] directory:
```
ugs deploy ./Access
```
You will find the resource from [sample-policy.ac] published in your dashboard for the configured project and environment.

### Create Access Files:

To create a deployable access file, you need a `.ac` file with the following pattern:
```Json
{
  "$schema": "https://ugs-config-schemas.unity3d.com/v1/project-access-policy.schema.json",
  "Statements": [
    {
      "Sid": "DenyAccessToAllServices",
      "Action": [
        "*"
      ],
      "Effect": "Allow",
      "Principal": "Player",
      "Resource": "urn:ugs:*",
      "Version": "1.0.0"
    }
  ]
}
```
Please take [sample-policy.ac] as an example. For more details, please check [Access Control Documentation Portal] and/or [Access Control schema].
## Deploy all Samples
Run command from [Samples/Deploy] directory:
```
ugs deploy .
```
You will find all the contents deployed in your dashboard for the configured project and environment. The command deploys all the samples in current (`Deploy`) directory for supported services.


-----------------------------------
## Deploy Triggers

Run command from [Samples/Deploy] directory:
```
ugs deploy ./Triggers
```
You will find the resources from [my-triggers.tr] published to your specified environment.

### Create Triggers Files:

To create a deployable trigger file, you need a `.tr` file.
The template can be created from the CLI using `ugs triggers new-file`.

Additionally, [my-triggers.tr] as an example. 
For more details, please check [Triggers Documentation Portal] and/or [Triggers Schema].

## Deploy all Samples
Run command from [Samples/Deploy] directory:
```
ugs deploy .
```
You will find all the contents deployed in your dashboard for the configured project and environment. The command deploys all the samples in current (`Deploy`) directory for supported services.


---
[Create a Module Project using the CLI]: https://docs.unity.com/ugs/en-us/manual/cloud-code/manual/modules/how-to-guides/write-modules/cli
[Create a Module Project]: https://docs.unity.com/ugs/en-us/manual/cloud-code/manual/modules/getting-started
[`deploy`]: https://services.docs.unity.com/guides/ugs-cli/latest/general/base-commands/deploy
[Remote Config files]: https://docs.unity3d.com/Packages/com.unity.remote-config@3.3/manual/Authoring/remote_config_files.html
[Leaderboards API]: https://services.docs.unity.com/leaderboards-admin/
[Leaderboards schema]: https://ugs-config-schemas.unity3d.com/v1/leaderboards.schema.json
[Economy resource schemas]: https://services.docs.unity.com/economy-admin/v2#tag/Economy-Admin/operation/addConfigResource
[Declare parameters in the script]: https://docs.unity.com/cloud-code/authoring-scripts-editor.html#Declare_parameters_in_the_script
[Module.ccm]: /Samples/Deploy/CloudCode/Module/Module.ccm
[Script.js]: /Samples/Deploy/CloudCode/Script/Script.js
[configuration.rc]: /Samples/Deploy/RemoteConfig/configuration.rc
[resource.ec]: /Samples/Deploy/Economy/resource.ec
[Samples/Deploy]: /Samples/Deploy
[Deploy Command]: https://services.docs.unity.com/guides/ugs-cli/latest/general/troubleshooting/project-roles#deploy-command
[Service Account]: https://services.docs.unity.com/docs/service-account-auth/index.html
[Login]: https://services.docs.unity.com/guides/ugs-cli/latest/general/base-commands/authentication/login
[configuration]: https://services.docs.unity.com/guides/ugs-cli/latest/general/base-commands/configuration/configuration-keys
[CURRENCY.ecc]: /Samples/Deploy/Economy/CURRENCY.ecc
[INVENTORY_ITEM.eci]: /Samples/Deploy/Economy/INVENTORY_ITEM.eci
[VIRTUAL_PURCHASE.ecv]: /Samples/Deploy/Economy/VIRTUAL_MONEY_PURCHASE.ecv
[REAL_MONEY_PURCHASE.ecr]: /Samples/Deploy/Economy/REAL_MONEY_PURCHASE.ecr
[Access Control Documentation Portal]: https://docs.unity.com/ugs/en-us/manual/overview/manual/access-control
[Access Control schema]: https://ugs-config-schemas.unity3d.com/v1/project-access-policy.schema.json
[sample-policy.ac]: /Samples/Deploy/ProjectAccess/sample-policy.ac
[Triggers Documentation Portal]: https://docs.unity.com/ugs/en-us/manual/cloud-code/manual/triggers
[Triggers Schema]: https://ugs-config-schemas.unity3d.com/v1/triggers.schema.json
[my-triggers.tr]: /Samples/Deploy/Triggers/my-triggers.tr
