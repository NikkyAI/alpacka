#!/bin/env bash
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cd $DIR/src/Alpacka.CLI
dotnet restore
dotnet build
dotnet publish -c release

lib=`ldconfig -p | grep -oE \/.+libgit2\.so.+`
cp -f "$lib" -T bin/release/netcoreapp1.1/publish/lib/linux/x86_64/libgit2-1196807.so

echo
echo "add the following line to your '.bashrc' or equivalent"
echo
echo "alias alpacka='$DIR/src/Alpacka.CLI/bin/release/netcoreapp1.1/alpacka.dll'"
