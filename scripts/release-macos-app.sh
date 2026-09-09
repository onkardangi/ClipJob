#!/bin/sh

set -eu

if [ -z "${CLIPJOB_SIGNING_IDENTITY:-}" ]; then
    echo "Set CLIPJOB_SIGNING_IDENTITY to a Developer ID Application identity." >&2
    exit 1
fi

if [ -z "${CLIPJOB_NOTARY_PROFILE:-}" ]; then
    echo "Set CLIPJOB_NOTARY_PROFILE to a notarytool Keychain profile." >&2
    exit 1
fi

repository_root=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
"$repository_root/scripts/build-macos-app.sh"
app_bundle="$repository_root/artifacts/macos/ClipJob.app"
entitlements="$repository_root/packaging/macos/ClipJob.entitlements"
version=$(/usr/libexec/PlistBuddy -c "Print :CFBundleShortVersionString" "$app_bundle/Contents/Info.plist")
submission_archive="$repository_root/artifacts/macos/ClipJob-notarization.zip"
distribution_archive="$repository_root/artifacts/macos/ClipJob-$version-macos-arm64.zip"

# Every bundled Mach-O file needs its own Developer ID signature. Sign the
# containing app last so its resource seal covers the final nested signatures.
find "$app_bundle/Contents/MacOS" -type f ! -name ClipJob -print0 |
    while IFS= read -r -d '' binary; do
        if file -b "$binary" | grep -q "Mach-O"; then
            codesign --force \
                --options runtime \
                --timestamp \
                --sign "$CLIPJOB_SIGNING_IDENTITY" \
                "$binary"
        fi
    done

codesign --force \
    --options runtime \
    --timestamp \
    --entitlements "$entitlements" \
    --sign "$CLIPJOB_SIGNING_IDENTITY" \
    "$app_bundle"

codesign --verify --deep --strict --verbose=2 "$app_bundle"

rm -f "$submission_archive" "$distribution_archive"
ditto -c -k --keepParent "$app_bundle" "$submission_archive"

xcrun notarytool submit "$submission_archive" \
    --keychain-profile "$CLIPJOB_NOTARY_PROFILE" \
    --wait

xcrun stapler staple "$app_bundle"
xcrun stapler validate "$app_bundle"
codesign --verify --deep --strict --verbose=2 "$app_bundle"
spctl --assess --type execute --verbose=4 "$app_bundle"

# Stapling changes the app bundle, so create the user-facing archive afterward.
ditto -c -k --keepParent "$app_bundle" "$distribution_archive"
rm -f "$submission_archive"

echo "$distribution_archive"
