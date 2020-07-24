#!/bin/bash

dotnet_runtime="osx-x64"
warp_runtime="macos-x64"
configuration="release"
git_commit=$(git log --format=%h --abbrev=7 -n 1)
publish_directory="../FreeMarketApp/bin/${configuration}/netcoreapp3.1/${dotnet_runtime}/publish"
download_directory="/tmp"
warp="${warp_runtime}.warp-packer"
project_path="../FreeMarketApp/FreeMarketApp.csproj"

echo warp is ${warp}
echo "Download directory is:" $download_directory
echo "Publish directory is: " $publish_directory
echo "Download file is " ${download_directory}/${warp}
echo "Current directory is:" $PWD 
echo "Git commit to build:" $git_commit

echo "Downloading warp..."
curl -L -o ${download_directory}/${warp} "https://github.com/dgiagio/warp/releases/download/v0.3.0/${warp}"

if [ -f "${download_directory}/${warp}" ]; then
   echo "Warp packer downloaded succesfully."
else
   echo "Warp packer didn't download successfully."
fi

echo "Building FreeMarketOne App..."
dotnet --info
dotnet publish $project_path -c $configuration -v m -r $dotnet_runtime 

echo "List of files to package:" 
ls $publish_directory

echo "Packaging the application..."
chmod +x "${download_directory}/${warp}"
"${download_directory}/./${warp}" --arch $warp_runtime --input_dir $publish_directory --exec FreeMarketApp --output ${publish_directory}/FreeMarketApp-$git_commit

echo "Making .app in $publish_directory/FreeMarketOne.app"

mkdir -p $publish_directory/FreeMarketOne.app/Contents/MacOS
mkdir -p $publish_directory/FreeMarketOne.app/Contents/Resources

cp $publish_directory/FreeMarketApp-$git_commit $publish_directory/FreeMarketOne.app/Contents/MacOS/
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
