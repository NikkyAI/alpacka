# Alpacka

## Requirements

System: any OS that supports dotnet core will be supported [supported runtime ids](https://docs.microsoft.com/en-us/dotnet/articles/core/rid-catalog#windows-rids)

As well as all systems that manage to somehow install the runtime and sdk will probably work as well

## libgit2 bug

At the moment only Distros based on Ubuntu are supported out of the box (or at least all git related commands are broken on the rest [#13](https://github.com/NikkyAI/alpacka/issues/13) )

### Temporary fix

#### manually for dev environment

replace the file `~/.nuget/packages/libgit2sharp.nativebinaries/1.0.165/runtimes/linux-x64/native/libgit2-1196807.so` with the libgit2.so file found on your system (probably in `/usr/lib/`)
and do not forget to rename it to `libgit2-1196807.so`

#### manually for releases

replace the file in the release folder
`publish/lib/linux/x86_64/libgit2-1196807.so`
with the proper libgit2.so file (and rename it to `libgit2-1196807.so`)

#### using a script

[Installation > sSimple Script](#simple-script)

## other requirements

It is recommended to install Git https://git-scm.com/downloads 

git is not required for just using install, update etc and no pack authoring functions

it is required for creating packs to have a proper git config set up (`user.name`, `user.email`, `core.editor`) as well as **`ssh keys`**

Install dotnet core https://www.microsoft.com/net/core#windowscmd
commandline edition for windows or follow the instruction for linux, maybe look in your package manager for `dotnet`, `dotnet-cli`, `dotnet-sdk`

you will need both the runtime and the sdk

## Installation

### Manually

since there are no releases for now, installing means cloning master and running the latest version

```bash
git clone git@github.com:NikkyAI/alpacka.git
cd alpacka
dotnet restore
```

> now apply the libgit2 fix

edit your `.bashrc`, `.zshrc` or equivalent   
it is recommended to use gitbash on windows as well.. so create a `.bashrc` file in your home folder

add the following line

```bash
alias alpacka-dev="dotnet run --project PATH_TO_SRC_FOLDER/alpacka/src/Alpacka.CLI/Alpacka.CLI.csproj"
```

to your bashrc or equivalent

replace `PATH_TO_SRC_FOLDER` to match your directory structure (in my case it is `/d/dev` so the full path is `/d/dev/alpacka/src/Alpacka.CLI/Alpacka.CLI.csproj`)

### simple script

make sure you have libgit2 installed

run `build.sh` in a terminal

the last line of the poutput will be like `/home/nikky/dev/alpacka/src/Alpacka.CLI/bin/release/netcoreapp1.1/alpacka.dll`

add that to your bashrc or equivalent

## Useage

try running `alpacka-dev --help`

install packs with `alpacka-dev install [multimc|server] [git-url]`   
only https urls are supported

`alpacka-dev init` is probably not updated to the newest pack versions

the rest is to be documented, help would be welcome

## Recommendations

do not use this yet.. it is not ready :P

editors:

- visual studio code
- sublime
- anything with yaml and json highlighters and git integration

terminal:

- terminator
- Git Bash (builtin to git on windows)
- vscode integrated terminal (can be configured to use git bash)

MC launcher:
- MultiMC (the only supported launcher atm)

git client / GUI:
- gitkraken https://www.gitkraken.com/
- git-gui (builtin)

## Support

almost wherever you catch the authors
