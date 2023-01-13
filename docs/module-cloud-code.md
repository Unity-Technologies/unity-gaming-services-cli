# Cloud-Code module

The Cloud-Code module is an interface to make calls to the Cloud-Code UGS service.

Root command aliases: `cloud-code`, `cc`

Use `ugs cloud-code -h` to get help on how to use the cloud-code commands.

- [Cloud-Code commands]
    - [Get]
    - [List]
    - [Create]
    - [Update]
    - [Delete]
    - [Publish]
- [Parameter Parsing]

# Commands

## Get

Get a Cloud-Code script.

Authentication Required: `Yes`

```bash
ugs cloud-code get <script-name> [options]
```

### Arguments

`<script-name>` The name of the script to get

### Options

|             | Alias                  | Description                         |
| ----------- | ---------------------- | ----------------------------------- |
| project-id  | -p, --project-id       | The Unity cloud project ID.         |
| environment | -e, --environment-name | The services environment name.      |
| help        | -?, -h, --help         | Display help and usage information. |
| quiet       | -q, --quiet            | Reduce logging to a minimum.        |
| json        | -j, --json             | Use JSON as the output format.      |

### JSON Output

You can use the `--json` option and JQuery to output the result of the get command to a file (script.js as example).
`ugs cloud-code get <script-name> [options] --json | jq -r '.Result.ActiveScript.Code' > script.js`

## List

List Cloud-Code scripts.

Authentication Required: `Yes`

```bash
ugs cloud-code list [options]
```

### Options

|             | Alias                  | Description                         |
| ----------- | ---------------------- | ----------------------------------- |
| project-id  | -p, --project-id       | The Unity cloud project ID.         |
| environment | -e, --environment-name | The services environment name.      |
| help        | -?, -h, --help         | Display help and usage information. |
| quiet       | -q, --quiet            | Reduce logging to a minimum.        |
| json        | -j, --json             | Use JSON as the output format.      |

## Create

Create a Cloud-Code script. Please check [Parameter Parsing] to see how the command parse parameter in script.

Authentication Required: `Yes`

```bash
ugs cloud-code create <script-name> <file-path> [options]
```

### Arguments

`<script-name>` The name of the script to create

`<file-path>` File path of the script to copy

### Options

|             | Alias                  | Description                         |
| ----------- | ---------------------- | ----------------------------------- |
| project-id  | -p, --project-id       | The Unity cloud project ID.         |
| environment | -e, --environment-name | The services environment name.      |
| type        | -t, --type             | Type of the target script.          |
| language    | -l, --language         | Language of the target script.      |
| help        | -?, -h, --help         | Display help and usage information. |
| quiet       | -q, --quiet            | Reduce logging to a minimum.        |
| json        | -j, --json             | Use JSON as the output format.      |

## Update

Update a Cloud-Code script. Please check [Parameter Parsing] to see how the command parse parameter in script.

Authentication Required: `Yes`

```bash
ugs cloud-code update <script-name> <file-path> [options]
```

### Arguments

`<script-name>` The name of the script to update

`<file-path>` File path of the script to copy

### Options

|             | Alias                  | Description                         |
| ----------- | ---------------------- | ----------------------------------- |
| project-id  | -p, --project-id       | The Unity cloud project ID.         |
| environment | -e, --environment-name | The services environment name.      |
| help        | -?, -h, --help         | Display help and usage information. |
| quiet       | -q, --quiet            | Reduce logging to a minimum.        |
| json        | -j, --json             | Use JSON as the output format.      |

## Delete

Delete Cloud-Code scripts.

Authentication Required: `Yes`

```bash
ugs cloud-code delete <script-name> [options]
```

### Arguments

`<script-name>` The name of the script to delete

### Options

|             | Alias                  | Description                         |
| ----------- | ---------------------- | ----------------------------------- |
| project-id  | -p, --project-id       | The Unity cloud project ID.         |
| environment | -e, --environment-name | The services environment name.      |
| help        | -?, -h, --help         | Display help and usage information. |
| quiet       | -q, --quiet            | Reduce logging to a minimum.        |
| json        | -j, --json             | Use JSON as the output format.      |

## Publish

Publish a Cloud-Code script.

Authentication Required: `Yes`

```bash
ugs cloud-code publish <script-name> [options]
```

### Arguments

`<script-name>` The name of the script to publish

### Options

|             | Alias                  | Description                         |
| ----------- | ---------------------- | ----------------------------------- |
| project-id  | -p, --project-id       | The Unity cloud project ID.         |
| environment | -e, --environment-name | The services environment name.      |
| version     | -v, --version          | Version of the script to republish. |
| help        | -?, -h, --help         | Display help and usage information. |
| quiet       | -q, --quiet            | Reduce logging to a minimum.        |
| json        | -j, --json             | Use JSON as the output format.      |

# Parameter Parsing

[Create] and [Update] command will evaluate parser parameter from cloud-code script. 

Look at sample [Script.js], parameter with name `range` and type `NUMERIC` are parsed and uploaded to dashboard. 

For more details, please check [Declare parameters in the script]

## Parsing Limitations

### Invalid Java Script Logic

Although [Create] and [Update] command parse and evaluate cloud code script, it does not detect invalid logic. For
example:

```
const _ = require("lodash-4.17");

const NUMBER_OF_SIDES = 6;

module.exports.params = {
    sides: "NUMERIC"
};

module.exports = async ({ params, context, logger }) => {
    ...
};

// Functions can exist outside of the script wrapper
function rollDice(sides) {
    return _.random(1, sides);
}
```

Although the script above have `module.exports.params` in code, it is overwritten by `module.exports = async ...`. As a
result it will have no parameter parsed.

[Cloud-Code commands]: #commands

[Get]: #get

[List]: #list

[Create]: #create

[Update]: #update

[Delete]: #delete

[Publish]: #publish

[Parameter Parsing]: #parameter-parsing

[Script.js]: /Samples/Deploy/CloudCode/Script.js

[Declare parameters in the script]: https://docs.unity.com/cloud-code/authoring-scripts-editor.html#Declare_parameters_in_the_script
