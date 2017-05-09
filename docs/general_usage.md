# General Usage

## Creating a pack
* `alpacka init <target> --name ... --description ...`
* [Fill __packconfig.yaml__ with mods and info]
* `alpacka build`
* `alpacka update`

## Installing an existing pack
* `alpacka install <target> <url>`

## Updating
or switching versions
* `alpacka update [version]`

to update to a specific version use eg `2.5.1`  
you can also update to `latest` or `recommended`  
or use a branch name like `master` to switch to that branch  
without a version given this will just reinstall the current version

## Development workflows

### switch to branch
for Switching to a branch run  
`alpacka update master`

### Edit Mods
for changing which mods are used edit __packconfig.yaml__

continue with [testing](#Testing)

### Editing Configs
or other files is easy..  
You can just save the files and run the game normally in most cases

> TODO: add info about how to edit files in features / filewatcher ?

* [edit files]
* [play and test]


### Testing

* `alpacka build`
* `alpacka update` - *reinstalls from packbuild*
* run instance

### Creating a Release
to automatically increase the last used version by 0.0.1 run  
`alpacka release --increase patch`  
this uses Semantic Versioning and the accepted values are  `major` , `minor` , `patch`

to create a specific version run  
`alpacka release 1.2.3`  
make sure you have __git__ installed and have __ssh keys__ set up so that you can push to origin
