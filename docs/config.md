# Configuration

example: https://github.com/NikkyAI/alpacka-sample/blob/master/packconfig.yaml

## Fields

#### Name

Type: string

#### Description

Type: string

#### Authors

Type: string[]

#### Links

Type: [links](#entrylinks)

#### MinecraftVersion

Type: Version identifier (string)

example: `1.7.10`


#### ForgeVersion

Type: build number, recommended or latest (string)

#### Defaults

Type: mapping of [resources](#entryresource) //TODO: use EntryMod and allow overriding of mod specifics

#### Includes

Type: Nested mapping of lists of [reosurces](#entryresource) //TODO: use EntryMod and allow overriding of mod specifics

uses they keys of [defaults](#defaults) to apply the default values to all mods in the mapping
This can be layered as long as lists and mappings are not mixed
they keys can also be linked using `&` eg. `mods & client & optional:` 
instead of

```yaml
mods:
  client:
    optional:
```

using the includes key can be useful to make sure mod lists are not mixed with the mapping

```yaml
includes:
  mods:
    client:
      - NoNausea

    optional:
      common:
        - OpenEye

      client:
        includes:
          - Neat

        latest:
          - Mapwriter 2
```

#### Features

WIP


## Types

### EntryResource

Values that can be set using [defaults](#defaults) and in [includes](#includes)

#### Handler

value: `curse` or `url`

#### Source

key: `src`

type: url, relative path (string)

#### MD5

type: string

#### Version

type: string

value: any string, recommended or latest

#### Path

relative location of the file
will be filled out by resource handlers if possible

#### Side

values: none, `client`, `server`, `both`

no value imples its side is `both`

### EntryMod

all fields from [EntryResource](#entryresource)

#### Name

type: string

#### Description

type: string

recommended to use yaml multiline strings

#### Links

type: [links](#entrylinks)


### EntryLinks

#### Website

Type: Url

#### Source

Type: Url

#### Issues

Type: Url

#### Dontations

Type: Url
