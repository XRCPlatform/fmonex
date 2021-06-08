#!/bin/bash

# Ensure you have wine32 installed.

set -e

dotnet_runtime="win-x64"
warp_runtime="win-x64"
configuration="Debug"
git_commit=$(git log --format=%h --abbrev=7 -n 1)
publish_directory="../FreeMarketApp/bin/${configuration}/netcoreapp3.1/${dotnet_runtime}/publish"
download_directory="/tmp"
project_path="../FreeMarketApp/FreeMarketApp.csproj"
ico_path="../FreeMarketApp/Assets/freemarket.ico"

version="1.1"

. common-windows.sh

#######

nsis_filename=nsis-3.06.1-setup.exe
nsis_url=https://downloads.sourceforge.net/project/nsis/NSIS%203/3.06.1/$nsis_filename
nsis_sha256=f60488a676308079bfdf6845dc7114cfd4bbff47b66be4db827b89bb8d7fdc52
cache_dir=./tmp-tools
export WINEPREFIX=$(pwd)/wine
export WINEDEBUG=-all

mkdir -p $WINEPREFIX

wine 'wineboot'
mkdir -p $cache_dir

pushd $cache_dir
if [ ! -f $nsis_filename ]; then
   wget $nsis_url
fi
popd

mkdir -p $WINEPREFIX/drive_c/fmone
cp -f -R $publish_directory/* $WINEPREFIX/drive_c/fmone

echo "$nsis_sha256 $cache_dir/$nsis_filename" | if sha256sum --check ; then
    echo "Building with nsis"
    if [ ! -f "$WINEPREFIX/drive_c/Program Files (x86)/NSIS/makensis.exe" ]; then
        wine "$cache_dir/$nsis_filename"
    fi
    wine "$WINEPREFIX/drive_c/Program Files (x86)/NSIS/makensis.exe" /DPRODUCT_VERSION=$version fmone-windows.nsi
else
    echo "nsis file was not the correct checksum" >&2
fi
