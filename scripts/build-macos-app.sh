#!/bin/sh

set -eu

repository_root=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
artifacts_directory="$repository_root/artifacts/macos"
publish_directory="$artifacts_directory/publish"
app_bundle="$artifacts_directory/ClipJob.app"

rm -rf "$publish_directory" "$app_bundle"
mkdir -p "$publish_directory" "$app_bundle/Contents/MacOS" "$app_bundle/Contents/Resources"

dotnet publish "$repository_root/src/ClipJob.Desktop/ClipJob.Desktop.csproj" \
    --configuration Release \
    --runtime osx-arm64 \
    --self-contained false \
    --output "$publish_directory" \
    -p:AssemblyName=ClipJob \
    -p:UseAppHost=true

cp "$repository_root/packaging/macos/Info.plist" "$app_bundle/Contents/Info.plist"
cp -R "$publish_directory/." "$app_bundle/Contents/MacOS/"
chmod +x "$app_bundle/Contents/MacOS/ClipJob"

# Ad-hoc signing gives this local development bundle a macOS code identity
# without requiring an Apple Developer certificate or distribution setup.
codesign --force --deep --sign - "$app_bundle"

echo "$app_bundle"
