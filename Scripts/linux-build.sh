#!/bin/bash

dotnet_runtime="linux-x64"
warp_runtime="linux-x64"
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

echo "Done."