# Publishing a ClipJob preview

The Preview release GitHub Actions workflow publishes a self-contained, ad-hoc-signed Apple Silicon archive whenever a matching preview tag is pushed. It uses no paid Apple services or repository secrets.

## Publish

1. Update `CFBundleShortVersionString` and `CFBundleVersion` in `packaging/macos/Info.plist` when the application version changes.
2. Add matching release notes at `docs/releases/vX.Y.Z-preview.N.md`.
3. Run `dotnet restore`, `dotnet build`, and `dotnet test`.
4. Commit and push the release preparation.
5. Create and push a matching tag, such as `v1.0.0-preview.1`.

The workflow verifies the tag, builds the app, verifies its ad-hoc signature, packages it, and creates a prerelease on GitHub.

Because the preview does not have a paid Developer ID signature or Apple notarization, the release notes and README must retain the manual Gatekeeper instructions. Do not describe an unsigned preview as Apple-verified.

The existing `scripts/release-macos-app.sh` remains available if Developer ID signing and notarization are configured in the future.
