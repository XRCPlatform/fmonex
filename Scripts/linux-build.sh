#!/bin/bash

dotnet_runtime="linux-x64"
warp_runtime="linux-x64"
configuration="Debug"
git_commit=$(git log --format=%h --abbrev=7 -n 1)
publish_directory="../FreeMarketApp/bin/${configuration}/netcoreapp3.1/${dotnet_runtime}/publish"
download_directory="/tmp"
project_path="../FreeMarketApp/FreeMarketApp.csproj"

. common.sh

#######

# Create a .deb file
mkdir -p pkg-debian pkg-debian/DEBIAN pkg-debian/usr/bin pkg-debian/usr/share/applications
touch pkg-debian/DEBIAN/{conffiles,control,md5sums,postinst,prerm} pkg-debian/debian-binary
chmod 555 pkg-debian/DEBIAN/postinst
chmod 555 pkg-debian/DEBIAN/prerm

cp ${publish_directory}/FreeMarketApp pkg-debian/usr/bin/freemarketone

cat <<EOF > pkg-debian/DEBIAN/control
Package: freemarketone
Version: 1.1
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
dpkg -b pkg-debian freemarketone-1.1_amd64.deb

rm -Rf pkg-debian

echo "Done."
