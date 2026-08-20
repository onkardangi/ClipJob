# ClipJob

ClipJob is a macOS-first desktop utility for quickly saving, organizing, searching, and pasting reusable text while completing job applications.

Instead of repeatedly opening a resume, previous application, notes file, or browser tab to find the same information, ClipJob gives frequently used application content a memorable label and makes it available through a global keyboard shortcut.

> **Core idea:** Give reusable application text a name, then paste it anywhere in seconds.

## Problem

Job applications repeatedly ask for the same information:

- LinkedIn and GitHub URLs
- Work history
- Education
- Role descriptions
- Technical experience
- Work authorization
- Salary expectations
- "Why this company?"
- "Why this role?"
- Project descriptions
- Behavioral stories

Traditional clipboard managers primarily answer: "What did I copy recently?"

ClipJob is intended to answer:

> **What reusable information do I want available while I am applying?**

## Product Vision

The primary interaction should eventually look like:

```text
Job application field focused
        ↓
⌘ + Shift + V
        ↓
ClipJob palette appears
        ↓
Search: "bect desc"
        ↓
@bectran-description
        ↓
Enter
        ↓
Original application regains focus
        ↓
Text is pasted
```

The interaction must be fast enough that using ClipJob feels nearly as natural as normal copy and paste.

## Example Clip Library

```text
PROFILE
├── @email
├── @phone
├── @linkedin
├── @github
└── @location

EXPERIENCE
├── @bectran-title
├── @bectran-dates
├── @bectran-description
├── @bectran-payments
└── @bectran-aws

EDUCATION
├── @uic-bs
├── @uic-ms
└── @graduation

COMMON ANSWERS
├── @why-company
├── @why-role
├── @next-role
├── @salary
├── @sponsorship
└── @relocation

STORIES
├── @proud-project
├── @technical-challenge
├── @production-incident
├── @leadership
└── @failure
```

The long-term goal is a reusable personal job-application knowledge library, not merely clipboard history.

# Development Strategy

ClipJob will be built incrementally.

The first objective is not to build the complete application. The first objective is to prove that the core macOS interaction works reliably.

We will use small implementation slices with explicit acceptance criteria and validate each slice before moving forward.

# Phase 0 — macOS Feasibility Spike

**Status: Complete.**

Before persistence, categories, AI, or application-specific features, prove:

```text
Chrome / Safari
      ↓
⌘ + Shift + V
      ↓
ClipJob palette
      ↓
Select hardcoded clip
      ↓
Enter
      ↓
Browser regains focus
      ↓
Selected text is pasted
```

The spike initially uses three hardcoded clips:

```text
email
test@example.com

linkedin
https://linkedin.com/in/test

experience
Built high-throughput REST APIs...
```

## Phase 0 Success Criteria

1. ClipJob runs successfully on macOS.
2. A global shortcut can summon ClipJob while another application is active.
3. The search palette accepts keyboard input.
4. Arrow keys can change the selected result.
5. Enter selects a clip.
6. Escape closes the palette.
7. ClipJob remembers the previously active application.
8. The previous application regains focus.
9. Selected text is pasted into the original field.
10. The workflow behaves reliably across common job-application environments.

Initial manual test targets:

- Chrome input
- Chrome textarea
- Safari
- Workday
- Greenhouse
- Lever
- LinkedIn

Clipboard preservation will also be investigated so using ClipJob does not unnecessarily replace the user's normal clipboard.

# Planned Technology Stack

| Area | Technology |
| --- | --- |
| Language | C# |
| Runtime | .NET 10 |
| Desktop UI | Avalonia UI |
| UI Pattern | MVVM |
| MVVM Utilities | CommunityToolkit.Mvvm |
| Local Storage | SQLite |
| Persistence Access | Decide after feasibility spike |
| Dependency Injection | Microsoft.Extensions.DependencyInjection |
| Logging | Microsoft.Extensions.Logging |
| Testing | xUnit |
| Primary Platform | macOS |
| Future Platform | Windows |

Persistence technology is intentionally deferred until after the feasibility spike.

## Local macOS App Bundle

On an Apple Silicon Mac, build the local development app bundle with:

```sh
./scripts/build-macos-app.sh
```

The script creates `artifacts/macos/ClipJob.app`. Launch it with
`open artifacts/macos/ClipJob.app`. To enable paste-back, add that app under
**System Settings → Privacy & Security → Accessibility**, enable it, then quit
and relaunch ClipJob.

# Architecture Principles

ClipJob is a small desktop utility. We want clear boundaries without enterprise ceremony.

## Platform Isolation

macOS-specific behavior must remain behind explicit platform abstractions.

Potential abstractions may eventually include:

```text
IGlobalHotkeyService
IClipboardService
IForegroundApplicationService
IPasteService
```

These are expected seams, not requirements to create them before needed.

Potential native integrations include AppKit, CoreGraphics, Accessibility APIs, and native global-hotkey APIs.

Avalonia handles application UI. Native macOS integration handles behavior that must operate outside the ClipJob process.

## Simplicity

Prefer:

- small cohesive classes
- explicit behavior
- straightforward control flow
- minimal dependencies
- testable boundaries

Avoid:

- generic repository frameworks
- unnecessary DTO/model/entity duplication
- speculative interfaces
- premature CQRS
- MediatR
- AutoMapper
- unnecessary event buses
- microservices
- cloud infrastructure

ClipJob remains local-first until a concrete requirement proves otherwise.

# Development Milestones

## Milestone 1 — Project Foundation

Create the smallest healthy Avalonia application:

- .NET 10
- Avalonia application
- macOS execution
- simple search textbox
- three hardcoded clips
- successful restore/build/run

No persistence or native integration.

## Milestone 2 — Search Palette

Implement:

- text filtering
- selected result
- Up/Down navigation
- Enter selection
- Escape dismissal

No global shortcut yet.

## Milestone 3 — Global macOS Shortcut

Register a system-wide shortcut such as `⌘ + Shift + V`.

It must summon ClipJob while another application has focus.

## Milestone 4 — Foreground Application Tracking

When ClipJob is summoned:

1. identify the active application,
2. remember it,
3. show ClipJob,
4. allow it to be restored later.

## Milestone 5 — Paste Back

Implement:

```text
Select clip
    ↓
Write temporary clipboard content
    ↓
Hide ClipJob
    ↓
Reactivate previous application
    ↓
Paste
```

This is the primary feasibility gate.

## Milestone 6 — Clipboard Preservation

Investigate:

```text
save existing clipboard
        ↓
temporarily write ClipJob text
        ↓
paste
        ↓
restore previous clipboard
```

Enable only if reliable.

## Milestone 7 — Reliability

Stress-test:

- repeated invocation
- rapid invocation
- multiline content
- large text
- special characters
- URLs
- emoji
- multiple browser windows
- full-screen applications
- cancellation
- empty clipboard
- common ATS websites

Do not advance to feature development until the core workflow is dependable.

# Phase 1 — Personal Clip Library

## Milestone 1 — Persistent Clip Storage

ClipJob stores clips in a local SQLite database in the user's application-data
directory, outside the repository. Clip-management UI is deferred to a later
milestone.

After the feasibility spike succeeds:

- create clip
- edit clip
- delete clip
- labels
- categories
- favorites
- search
- character count
- recently used clips
- usage count
- SQLite persistence

The application remains local-first and requires no account.

# Phase 2 — Job Application Features

Features should be driven by real usage.

Potential capabilities:

## Answer Variants

```text
@proud-project

SHORT
< 250 characters

MEDIUM
< 500 characters

LONG
< 1500 characters
```

## Aliases

Multiple search terms can resolve to the same clip:

```text
recruiter
intro
recruiter-message
```

## Application-Oriented Categories

Examples:

- Profile
- Experience
- Education
- Common Answers
- Projects
- Behavioral Stories
- Work Authorization

## Character-Limit Assistance

Quickly choose an answer appropriate for a form's character limit.

# Phase 3 — Intelligent Assistance

AI is intentionally excluded from the initial product.

Future versions may support:

- recognizing application question types
- suggesting relevant saved clips
- finding semantically similar previous answers
- adapting an existing answer to a company or role
- shortening an answer to a character limit
- composing answers from user-approved source material

User-stored information remains the source of truth. AI must not invent employment history, projects, accomplishments, education, or other personal facts.

# Non-Goals

ClipJob is not currently intended to be:

- a general-purpose clipboard-history manager
- a resume builder
- a job-search engine
- a job tracker
- an ATS scraper
- a browser autofill replacement
- an AI application bot
- a cloud service
- a social platform

# Privacy

Job application content may include sensitive personal information.

The initial application should be:

- local-first
- explicit about what is saved
- free from automatic clipboard-history collection
- free from unnecessary telemetry
- free from cloud synchronization

Only content the user intentionally saves should be persisted.

Passwords, authentication tokens, API keys, and financial information should not be treated as ordinary clips.

# Engineering Quality

AI-assisted development may be used, including Codex, but generated code is held to the same standard as manually written code.

Every implementation slice should:

1. have explicit scope,
2. have explicit acceptance criteria,
3. build successfully,
4. run relevant tests,
5. report warnings,
6. review the resulting diff,
7. avoid unrelated changes.

Actively reject:

- unnecessary abstraction
- speculative extensibility
- excessive comments
- package proliferation
- fake or low-value tests
- giant helper/manager classes
- duplicate models
- broad exception swallowing
- dead code
- scope creep
- cargo-cult design patterns

The goal is a small, understandable, reliable codebase.

# AI Development Workflow

```text
Define milestone
      ↓
Define acceptance criteria
      ↓
Codex implements
      ↓
Build
      ↓
Tests
      ↓
Review diff
      ↓
Architecture review
      ↓
Manual validation
      ↓
Commit
```

Do not ask an AI coding agent to implement future milestones simply because it can.

Human approval remains the merge gate.

A LangGraph-based development workflow may eventually automate repetitive implementation, validation, and review steps, but it is not part of the initial development effort.

# Current Status

**Current phase:** Phase 1 — Personal Clip Library

**Current milestone:** Milestone 1 — Persistent Clip Storage

The immediate goal is to load the existing palette's clips from persistent
local SQLite storage while preserving the validated Phase 0 workflow.

# Guiding Principle

> **Build the smallest thing that proves the next important assumption.**

For ClipJob, reliability and speed matter more than feature count.
