# Commands

In doubt check `alpacka --help`

## `alpacka`

Run this to get started

Options

| Short | Long        | Description |
|-------|-------------|-------------|
|       | `--version` | Displays the current alpacka version. |
|       | `--help`    | Provides general help or help for a specific command. |

## `alpacka init`

Initializes a new alpacka packconfig.yaml and git repo

| Required | Argument      | Description |
|----------|---------------|-------------|
| X        | `type`        | Instance Type to install. [Instance Types](#instance-types) |
| X        | `name`        | Name of the modpack, is used as the directory of the pack instance |


## `alpacka install`

Creates a new Minecraft instance.

| Required | Argument      | Description |
|----------|---------------|-------------|
| X        | `type`        | Instance Type to install. [Instance Types](#instance-types) |
| X        | `repository`  | Git repository of the alpacka modpack to install. |
|          | `[directory]` | Directory or name to install the pack into. Defaults to pack name.  (wip) |

Options

| Short | Long          | Description |
|-------|---------------|-------------|
|       | `--no-update` | Don't call alpacka update afterwards. |

Lists possible targets when just alpacka install is called.


## `alpacka build`

Reads the packconfig.yaml, downloads the most recent versions of mods and creates or updates the packbuild.json.


## `alpacka update`

Updates a alpacka modpack and downloads mods.

| Required | Argument    | Description |
|----------|-------------|-------------|
|          | `[version]` | Version to update to. Can be "recommended", "latest" or a release (git tag), branch, commit or any git commit-ish. (branch, commit, 'HEAD~1' etc.) |

Internally, this command does a git checkout.

 Without a version specified, redownloads missing mods and removes duplicates (with user interaction?). This can also be used (automatically) right after a clone.

Options

| Short | Long     | Description |
|-------|----------|-------------------------------|
| `-l`  | `--list` | Lists all available versions. |


## `alpacka release`

*TODO: Check code to see if this is correct*
Creates a release of the pack from the packconfig.yaml.  

| Required | Argument    | Description |
|----------|-------------|-------------|
|          | `[version]` | Version of this release. |

Options

| Short | Long          | Description |
|-------|---------------|-------------|
|       | `--no-commit` | Don't commit, just puts a tag on the current commit (tip) does not work with `--build` |
|       | `--build`     | runs `build` and generates the packbuild.json, needs to create a extra commit|
|       | `--no-push`   | Don't push the newly created tag to the remote repository.  |

Unless `--no-commit` is set, after the packbuild.json was written, this creates commit that includes the packbuild.json, a tag with the specified version. Errors if there are staged or unstaged changes

## `alpacka import`

Imports a curse modpack and creates a alpacka pack from it

| Required | Argument    | Description |
|----------|-------------|-------------|
| X        | `type`      | Instance Type to install. [Instance Types](#instance-types) |
| X        | `zip`       | Url of the curse modpack download |


## `alpacka export`

???

## `alpacka clear-cache`

Deletes the alpacka cache folder.

## Notes

### Instance Types

| Type      | Description                                                       |
|-----------|-------------------------------------------------------------------|
| `vanilla` | ~~Installs an instance into the Vanilla Minecraft launcher.~~ WIP |
| `multimc` | Creates an instance in MultiMC.                                   |
| `server`  | Creates a Minecraft server set up with the pack.                  |
