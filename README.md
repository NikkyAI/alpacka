# Alpacka

## Requirements

System: any OS that supports dotnet core will be supported [supported runtime ids](https://docs.microsoft.com/en-us/dotnet/articles/core/rid-catalog#windows-rids)   
and all systems that manage to somehow install the runtime and sdk will probably work as well   
At the Moment due to a bug Centos7 and Archlinux are not supported (or at least all git related commands are broken [#13](../../issues/13) )

It is recommended to install Git https://git-scm.com/downloads (actually required for now)
it is required for creating packs to have a proper git config set up (`user.name`, `user.email`, `core.editor`) as well as **`ssh keys`**

Install dotnet core https://www.microsoft.com/net/core#windowscmd
commandline edition for windows or follow the instruction for linux, maybe look in your package manager for `dotnet`, `dotnet-cli`, `dotnet-sdk`   
you will need both the runtime and the sdk

## Installation

since there are no releases for now, installing means cloning master and running the latest version

```bash
git clone git@github.com:NikkyAI/alpacka.git
cd alpacka
dotnet restore
```

edit your `.bashrc`, `.zshrc` or equivalent   
it is recommended to use gitbash on windows as well.. so create a `.bashrc` file in your home folder

add the following line
```bash
alias alpacka-dev="dotnet run --project PATH_TO_SRC_FOLDER/alpacka/src/Alpacka.CLI/Alpacka.CLI.csproj"
```
to your bashrc or equivalent

replace `PATH_TO_SRC_FOLDER` to match your directory structure (in my case it is `/d/dev` so the full path is `/d/dev/alpacka/src/Alpacka.CLI/Alpacka.CLI.csproj`)

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

MC launcher:
- MultiMC (the only supported launcher atm)

git client / GUI:
- gitkraken https://www.gitkraken.com/
- git-gui (builtin)

## Support

almost wherever you catch the authors
