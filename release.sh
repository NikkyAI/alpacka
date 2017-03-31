#!/usr/bin/env bash

# go get github.com/aktau/github-release

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

function copy_publish() {
    PROJECT=$1
    TARGET=$2
    
    PUBLISH_DIR="$DIR/src/$PROJECT/bin/release/$TargetFramework/$TARGET/publish"
    TARGET=${TARGET:-any}
    TARGET_DIR="$DIR/release/$PROJECT-$TARGET/gitmc"
    
    rm -r $TARGET_DIR
    mkdir $TARGET_DIR --parent
    echo "'$PUBLISH_DIR' -> '$TARGET_DIR'"
    cp -rf $PUBLISH_DIR -T $TARGET_DIR
    
    zipfile="$DIR/release/$PROJECT-$TARGET.zip"
    rm $zipfile
    cd $DIR/release/$PROJECT-$TARGET/
    zip -fdr $zipfile *
    
    github-release upload \
        --user NikkyAi \
        --repo gitmc \
        --tag $tag \
        --label $TARGET \
        --name "$PROJECT-$TARGET-$tag.zip" \
        --file "$zipfile" #\
        #--replace
}

function release_runtime() {
    PROJECT=$1
    RUNTIME=$2
    
    cd "$DIR/src/$PROJECT"
    dotnet restore -r $RUNTIME
    dotnet build -r $RUNTIME
    dotnet publish -c release -r $RUNTIME
    
    copy_publish $PROJECT $RUNTIME
}

function load_csproj() {
    PROJECT=$1
    data=`cat "$DIR/src/$PROJECT/$PROJECT.csproj"`
    TargetFramework=`sed -n -e 's/.*<TargetFramework>\(.*\)<\/TargetFramework>.*/\1/p' <<< $data `
    RuntimeIdentifiers=`sed -n -e 's/.*<RuntimeIdentifiers>\(.*\)<\/RuntimeIdentifiers>.*/\1/p' <<< $data `
    RuntimeIdentifiers=$(echo $RuntimeIdentifiers | tr ";" "\n")
    VersionPrefix=`sed -n -e 's/.*<VersionPrefix>\(.*\)<\/VersionPrefix>.*/\1/p' <<< $data `
}

function release() {
    PROJECT=$1
    cd "$DIR/src/$PROJECT"
    load_csproj $PROJECT
    
    dotnet restore
    dotnet build
    dotnet publish -c release
    
    tag="v$VersionPrefix"

    github-release release \
        --user NikkyAi \
        --repo gitmc \
        --tag $tag \
        --pre-release
        
    copy_publish $PROJECT ""
    
    for id in $RuntimeIdentifiers
        do
            echo "> [$id]"
            release_runtime $PROJECT $id
        done
    
    cd $DIR
}
branch=$(git symbolic-ref --short -q HEAD)
echo "branch: $branch"
if [ "$branch" != "master" ]; then
    echo "you are not on master"
    exit 1;
fi

release GitMC.CLI
