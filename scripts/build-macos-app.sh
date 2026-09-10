#!/bin/sh

set -eu

repository_root=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
artifacts_directory="$repository_root/artifacts/macos"
publish_directory="$artifacts_directory/publish"
app_bundle="$artifacts_directory/ClipJob.app"

rm -rf "$publish_directory" "$app_bundle"
# Prevent macOS from presenting development bundles in Launchpad or Spotlight
# alongside the installed copy in /Applications.
mkdir -p "$artifacts_directory"
touch "$artifacts_directory/.metadata_never_index"
mkdir -p "$publish_directory" "$app_bundle/Contents/MacOS" "$app_bundle/Contents/Resources"

dotnet publish "$repository_root/src/ClipJob.Desktop/ClipJob.Desktop.csproj" \
    --configuration Release \
    --runtime osx-arm64 \
    --self-contained true \
    --output "$publish_directory" \
    -p:AssemblyName=ClipJob \
    -p:UseAppHost=true

cp "$repository_root/packaging/macos/Info.plist" "$app_bundle/Contents/Info.plist"
cp -R "$publish_directory/." "$app_bundle/Contents/MacOS/"
chmod +x "$app_bundle/Contents/MacOS/ClipJob"

# Keep the local development identity stable across rebuilds so macOS does not
# invalidate ClipJob's Accessibility grant whenever the executable hash changes.
codesign --force --deep --sign - "$app_bundle"
codesign --force --sign - \
    --requirements '=designated => identifier "com.clipjob.app"' \
    "$app_bundle"

launch_services_register="/System/Library/Frameworks/CoreServices.framework/Frameworks/LaunchServices.framework/Support/lsregister"
if ! "$launch_services_register" -u "$app_bundle"; then
    echo "Warning: could not unregister the development bundle from Launch Services." >&2
fi

echo "$app_bundle"
