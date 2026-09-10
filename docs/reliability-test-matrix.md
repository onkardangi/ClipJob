# ClipJob reliability test matrix

This checklist validates the workflow that matters most: summon ClipJob over another application, find a clip, return focus, and paste without disturbing the user's clipboard. Native macOS behavior requires manual verification in addition to the automated test suite.

## Recording a test run

Before a run, record:

- ClipJob version or commit
- macOS version and Mac model
- display arrangement and active Space
- browser/application version
- tester and date

Use one of these statuses in the tables: `Not run`, `Pass`, `Fail`, or `Blocked`. For failures, link an issue or add concise reproduction notes in the Evidence column. Do not mark a test passed without performing it on the recorded environment.

## Core workflow

| ID | Scenario | Expected result | Status | Evidence |
| --- | --- | --- | --- | --- |
| CORE-01 | Invoke the configured shortcut from a focused text field | Palette appears above the active application with search focused | Not run | — |
| CORE-02 | Search using part of a clip label | Matching clips appear immediately in the expected order | Not run | — |
| CORE-03 | Search using text that exists only in clip content | The matching clip appears | Not run | — |
| CORE-04 | Navigate results with Up and Down, then press Enter | Selected clip is pasted into the original field | Not run | — |
| CORE-05 | Open the palette and press Escape | Palette hides and no text is pasted | Not run | — |
| CORE-06 | Copy text, paste a ClipJob clip, then paste normally | Original clipboard text is available again | Not run | — |
| CORE-07 | Paste when the original clipboard contains no text | Clip is pasted without an error; clipboard remains usable | Not run | — |
| CORE-08 | Rapidly invoke, dismiss, and invoke the palette again | Only one responsive palette is shown | Not run | — |

## Browsers and forms

Run each case in a plain input, a multiline textarea, and at least one real job-application form that the tester is authorized to use.

| ID | Target | Expected result | Status | Evidence |
| --- | --- | --- | --- | --- |
| WEB-01 | Google Chrome | Focus returns to the same field and the clip is pasted once | Not run | — |
| WEB-02 | Safari | Focus returns to the same field and the clip is pasted once | Not run | — |
| WEB-03 | Firefox | Focus returns to the same field and the clip is pasted once | Not run | — |
| WEB-04 | Content-editable rich-text field | Text is inserted once without unexpected formatting | Not run | — |
| WEB-05 | Workday application form | Search, focus restoration, and paste complete successfully | Not run | — |
| WEB-06 | Greenhouse application form | Search, focus restoration, and paste complete successfully | Not run | — |
| WEB-07 | Lever application form | Search, focus restoration, and paste complete successfully | Not run | — |

## Displays, Spaces, and window behavior

| ID | Scenario | Expected result | Status | Evidence |
| --- | --- | --- | --- | --- |
| WIN-01 | Invoke from an application on the primary display | Palette appears on the primary display | Not run | — |
| WIN-02 | Invoke from an application on a secondary display | Palette appears on that secondary display | Not run | — |
| WIN-03 | Move the active application between displays, then invoke | Palette follows the application's current display | Not run | — |
| WIN-04 | Invoke from a full-screen application in another Space | Palette appears in the active full-screen Space | Not run | — |
| WIN-05 | Drag and resize the palette | Window remains usable within its minimum and maximum size | Not run | — |
| WIN-06 | Edit or delete a clip while the palette floats | Dialog stays visible and returns focus to the palette when closed | Not run | — |

## Settings, permissions, and lifecycle

| ID | Scenario | Expected result | Status | Evidence |
| --- | --- | --- | --- | --- |
| LIFE-01 | Launch without Accessibility permission | Settings reports missing permission and paste shows a useful error | Not run | — |
| LIFE-02 | Grant Accessibility permission and relaunch | Settings reports permission granted and paste succeeds | Not run | — |
| LIFE-03 | Change the global shortcut and restart ClipJob | The saved shortcut still summons ClipJob | Not run | — |
| LIFE-04 | Choose a shortcut owned by another application | Conflict is reported and the previous shortcut still works | Not run | — |
| LIFE-05 | Create, edit, and delete clips, then restart | All completed changes persist exactly once | Not run | — |
| LIFE-06 | Install the preview ZIP on a Mac without .NET | App opens after documented Gatekeeper approval | Not run | — |
| LIFE-07 | Show, open Settings, and quit from the menu bar | Each action works while ClipJob remains absent from the Dock | Not run | — |

## Automated baseline

The xUnit suite covers filtering, ordering, selection, validation, clip persistence, settings persistence, clipboard recovery, and paste-workflow state transitions. GitHub Actions runs restore, build, and tests on macOS for every push and pull request.

Before a manual test session, confirm this baseline:

```sh
dotnet restore
dotnet build
dotnet test
```

Record the command result and CI run with the completed manual matrix. Automated tests do not replace the browser, Accessibility, focus, display, and Space checks above.
