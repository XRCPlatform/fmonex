echo "Download directory is:" $download_directory
echo "Publish directory is: " $publish_directory
echo "Current directory is:" $PWD
echo "Git commit to build:" $git_commit

echo "Building FreeMarketOne App..."
pushd $(dirname $project_path)
dotnet --info
dotnet publish --configuration $configuration --self-contained=true -v m -r $dotnet_runtime -p:PublishTrimmed=true -p:PublishSingleFile=true
popd

echo "List of files to package:"
ls $publish_directory
