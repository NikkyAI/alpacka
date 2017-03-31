#!/usr/bin/env bash
VERSION=${1:-any}
# currently does not work because it is private
url=`curl -s https://api.github.com/repos/nikkyai/gitmc/releases/latest | grep browser_download_url | grep $VERSION | head -n 1 | cut -d '"' -f 4`

echo $url

github-release info \
    --user nikkyai \
    --repo gitmc