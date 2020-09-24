#!/bin/bash

dotnet_runtime="osx-x64"
configuration="release"
git_commit=$(git log --format=%h --abbrev=7 -n 1)
publish_directory="../FreeMarketApp/bin/${configuration}/netcoreapp3.1/${dotnet_runtime}/publish"
download_directory="/tmp"
project_path="../FreeMarketApp/FreeMarketApp.csproj"

. common.sh

#######

echo "Making .app in $publish_directory/FreeMarketOne.app"

mkdir -p $publish_directory/FreeMarketOne.app/Contents/MacOS
mkdir -p $publish_directory/FreeMarketOne.app/Contents/Resources

cp $publish_directory/FreeMarketApp $publish_directory/FreeMarketOne.app/Contents/MacOS/
cat <<INFO > $publish_directory/FreeMarketOne.app/Contents/Info.plist
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple Computer//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleGetInfoString</key>
  <string>FreeMarketOne</string>
  <key>CFBundleExecutable</key>
  <string>FreeMarketApp-$git_commit</string>
  <key>CFBundleIdentifier</key>
  <string>org.fmone</string>
  <key>CFBundleName</key>
  <string>FreeMarketApp-$git_commit</string>
  <key>CFBundleIconFile</key>
  <string>foo.icns</string>
  <key>CFBundleShortVersionString</key>
  <string>0.01</string>
  <key>CFBundleInfoDictionaryVersion</key>
  <string>6.0</string>
  <key>CFBundlePackageType</key>
  <string>APPL</string>
  <key>IFMajorVersion</key>
  <integer>0</integer>
  <key>IFMinorVersion</key>
  <integer>1</integer>
</dict>
</plist>
INFO


echo "Making .dmg in $publish_directory/FreeMarketOne.dmg"
hdiutil create -fs HFS+ -volname FreeMarketOne.dmg -srcfolder $publish_directory/FreeMarketOne.app $publish_directory/FreeMarketOne.dmg || fail "Could not create .DMG"

echo "Done."
