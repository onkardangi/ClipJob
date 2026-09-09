# ClipJob

ClipJob is a macOS-first desktop utility for saving, finding, and pasting reusable text while completing job applications.

Instead of repeatedly searching a resume, notes file, or previous application, users give frequently used text a memorable label and retrieve it from a keyboard-driven palette.

```text
Application field focused
        ↓
⌘⇧V
        ↓
Search and select a clip
        ↓
Return to the application and paste
```

## Current status

ClipJob is a working local prototype. The macOS workflow and the first personal-library milestone are implemented:

- global `⌘⇧V` shortcut
- keyboard-driven search by label or content
- arrow-key selection, Enter to paste, and Escape to dismiss
- create, edit, and delete with confirmation
- immediate palette updates after clip changes
- SQLite persistence across restarts
- foreground-application restoration and synthetic paste
- clipboard text restoration after paste
- movable and resizable palette
- self-contained Apple Silicon application bundle

The local development build is ad-hoc signed. A separate release workflow produces a Developer ID-signed and notarized archive when Apple Developer credentials are configured.

## Why ClipJob

Job applications repeatedly request the same information:

- contact and profile links
- work history and role descriptions
- education
- project summaries
- work authorization
- salary and relocation expectations
- answers such as “Why this company?”
- behavioral stories

Clipboard-history tools answer “What did I copy recently?” ClipJob instead answers:

> What reusable information do I want available while I am applying?

Only clips intentionally created by the user are stored. ClipJob does not collect clipboard history, require an account, or synchronize data to the cloud.

## Keyboard shortcuts

| Shortcut | Action |
| --- | --- |
| `⌘⇧V` | Show ClipJob |
| `↑` / `↓` | Change the selected clip |
| `Enter` | Paste the selected clip |
| `Escape` | Hide the palette or cancel a dialog |
| `⌘N` | Create a clip |
| `⌘E` | Edit the selected clip |
| `⌘⌫` | Delete the selected clip |
| `⌘Enter` | Save from the clip editor |

## Technology

| Area | Implementation |
| --- | --- |
| Language and runtime | C# and .NET 10 |
| Desktop UI | Avalonia UI 12 |
| Persistence | SQLite through `Microsoft.Data.Sqlite` |
| Testing | xUnit |
| Native integration | Carbon, AppKit/Foundation runtime messaging, and CoreGraphics |
| Primary platform | macOS |

The codebase deliberately avoids a dependency-injection container, ORM, and general-purpose application frameworks. Platform-specific behavior is kept behind small interfaces where the current workflow needs a boundary.

## Repository layout

```text
src/ClipJob.Desktop/          Avalonia application and macOS integrations
tests/ClipJob.Desktop.Tests/  View-model, persistence, and workflow tests
packaging/macos/              macOS application metadata
scripts/                      Local packaging scripts
```

Important implementation boundaries include:

- `MainWindowViewModel` — filtering, selection, validation, and in-memory palette state
- `SqliteClipRepository` — local schema initialization and clip persistence
- `PasteBackWorkflow` — clipboard replacement, application restoration, paste, and clipboard recovery
- `MacOSGlobalHotkeyService` — Carbon global-shortcut registration
- `MacOSForegroundApplicationService` — foreground-process capture and restoration
- `MacOSOverlayWindowService` — floating-window and Space behavior
- `MacOSPasteService` — synthetic Command-V events

## Build and test

Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0), then run:

```sh
dotnet restore
dotnet build
dotnet test
```

## Build the macOS app

On an Apple Silicon Mac:

```sh
./scripts/build-macos-app.sh
open artifacts/macos/ClipJob.app
```

The script creates an ad-hoc-signed, self-contained app at `artifacts/macos/ClipJob.app`. The bundle includes the .NET runtime, so a destination Mac does not need .NET installed. It currently targets `osx-arm64` only.

### Build a signed and notarized release

A public distribution requires an Apple Developer Program membership, a
`Developer ID Application` certificate installed in Keychain, and notarization
credentials stored with Apple's `notarytool`:

```sh
xcrun notarytool store-credentials "ClipJob-notary" \
  --apple-id "you@example.com" \
  --team-id "YOUR_TEAM_ID"
```

After `notarytool` prompts for an app-specific password and saves the profile,
create the release archive with:

```sh
CLIPJOB_SIGNING_IDENTITY="Developer ID Application: Your Name (TEAMID)" \
CLIPJOB_NOTARY_PROFILE="ClipJob-notary" \
./scripts/release-macos-app.sh
```

The release script:

1. builds the self-contained Apple Silicon app
2. signs its native binaries with hardened runtime enabled
3. signs the app with the .NET JIT entitlement
4. submits it to Apple's notary service and waits for acceptance
5. staples and validates the notarization ticket
6. verifies the app with `codesign` and Gatekeeper
7. creates `artifacts/macos/ClipJob-1.0-macos-arm64.zip`

Credentials remain in the macOS Keychain and are not written to the repository.

### Enable paste-back

Synthetic paste requires Accessibility permission:

1. Build and open `artifacts/macos/ClipJob.app`.
2. Open **System Settings → Privacy & Security → Accessibility**.
3. Add and enable `ClipJob.app`.
4. Quit and reopen ClipJob.
5. Focus a text field in another application and press `⌘⇧V`.

## Local data and privacy

Clips are stored at:

```text
~/Library/Application Support/ClipJob/clipjob.db
```

The database is outside the repository and remains on the local Mac. A new empty database receives three example clips containing placeholder data.

ClipJob does not currently include telemetry, authentication, cloud synchronization, or automatic clipboard-history collection. Passwords, tokens, financial information, and other secrets should not be stored as ordinary clips.

## Known limitations

- On multi-monitor systems, the palette can remain on a different display from the active application. Cross-Space floating is implemented, but active-display placement is not.
- Native paste behavior still requires manual testing across browsers, full-screen Spaces, and job-application sites.
- Release bundles are currently Apple Silicon-only. Developer ID signing and notarization require locally configured Apple Developer credentials.
- Clipboard preservation currently snapshots text. It cannot reconstruct non-text clipboard formats.
- The global shortcut is fixed at `⌘⇧V` and may conflict with another application.

## Roadmap

Completed:

- macOS feasibility workflow
- search palette and keyboard navigation
- global shortcut and foreground-app tracking
- paste-back and clipboard text preservation
- persistent SQLite clip storage
- create, edit, and delete workflows
- Developer ID signing and notarization workflow

Likely next work:

- position the palette on the active application’s display
- broaden reliability testing across browsers and ATS websites
- add a user-facing quit/settings surface
- publish the first versioned, notarized GitHub release

Future product work may include categories, favorites, aliases, answer variants, character-limit assistance, and usage-based organization. These are intentionally excluded until the core workflow is dependable.

## Engineering approach

ClipJob is developed in small milestones with explicit acceptance criteria. Reliability and speed matter more than feature count.

The project favors:

- simple, cohesive classes
- explicit platform boundaries
- minimal dependencies
- observable behavior tests
- focused diffs and manual macOS validation

See [`AGENTS.md`](AGENTS.md) for the repository’s detailed engineering rules.
