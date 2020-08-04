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

# Create a .deb file
mkdir -p pkg-debian pkg-debian/DEBIAN pkg-debian/usr/bin pkg-debian/usr/share/applications
touch pkg-debian/DEBIAN/{conffiles,control,md5sums,postinst,prerm} pkg-debian/debian-binary
chmod 555 pkg-debian/DEBIAN/postinst
chmod 555 pkg-debian/DEBIAN/prerm

cp ${publish_directory}/FreeMarketApp-$git_commit pkg-debian/usr/bin/freemarketone

cat <<EOF > pkg-debian/DEBIAN/control
Package: freemarketone
Version: 0.0.1
Architecture: amd64
Essential: no
Section: web
Maintainer: FreeMarketOne Developers
Installed-Size: 49000
Description: Decentralized precious metal marketplace.
EOF

cat <<EOF > pkg-debian/usr/share/applications/freemarketone.desktop
[Desktop Entry]
Name=FreeMarketOne
Comment=Decentralized precious metal marketplace.
Exec=/usr/bin/freemarketone
Terminal=false
Type=Application
Categories=Unknown
EOF

cd pkg-debian/
find . -type f ! -regex '.*.git.*' ! -regex '.*?debian-binary.*' ! -regex '.*?DEBIAN.*' -printf '%P ' | xargs md5sum > DEBIAN/md5sums
cd ..
dpkg -b pkg-debian freemarketone-0.0.1_amd64.deb

rm -Rf pkg-debian

echo "Done."
