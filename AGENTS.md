# ClipJob Engineering Instructions

## Purpose

This file defines the engineering rules AI coding agents must follow when working in the ClipJob repository.

Read this file and `README.md` completely before making changes.

`README.md` defines what we are building.

`AGENTS.md` defines how the codebase must be changed.

# Project Goal

ClipJob is a macOS-first desktop utility for saving, searching, and quickly pasting reusable text while completing job applications.

The primary product interaction is:

1. User is working in another application.
2. User invokes ClipJob with a global shortcut.
3. ClipJob displays a keyboard-driven search palette.
4. User selects a saved clip.
5. ClipJob restores the previous application.
6. The selected text is pasted into the previously focused field.

Reliability and speed are more important than feature count.

# Development Philosophy

Build the smallest implementation that proves the current milestone.

Do not implement future milestones unless explicitly requested.

Prefer:

- simple code
- explicit behavior
- small cohesive classes
- minimal dependencies
- clear ownership
- testable boundaries

Avoid speculative architecture.

Do not optimize for how much code is produced. Optimize for how little correct code is necessary.

# Current Technical Direction

Primary platform:

- macOS

Technology:

- C#
- .NET 10
- Avalonia UI
- MVVM where useful
- CommunityToolkit.Mvvm when justified
- SQLite later
- xUnit for tests

Native macOS integration will eventually be required for:

- global keyboard shortcuts
- foreground application tracking
- application activation
- synthetic paste behavior
- accessibility-related functionality

Do not implement these before their respective milestones.

# Architecture Rules

## Platform Isolation

macOS-specific APIs must remain behind explicit platform boundaries.

UI code must not directly contain native macOS implementation details.

Potential abstractions may eventually include:

```text
IGlobalHotkeyService
IClipboardService
IForegroundApplicationService
IPasteService
```

These are examples of likely boundaries, not instructions to create these interfaces immediately.

Do not create an abstraction until the current milestone requires it.

## UI

Do not place substantial application behavior in code-behind.

Code-behind may contain trivial view-specific behavior when appropriate.

Do not create unnecessary UI frameworks or abstraction layers.

UI code should primarily be responsible for presentation, user interaction, and view state.

Platform behavior and application logic should live elsewhere when that separation provides concrete value.

## Domain

Domain objects should remain independent of:

- Avalonia
- macOS APIs
- persistence libraries
- dependency-injection frameworks

Do not create a separate domain project until the application has enough domain behavior to justify one.

## Persistence

Persistence is not part of the feasibility spike.

Do not add:

- SQLite
- EF Core
- Dapper
- repositories
- database migrations

until persistence is explicitly requested.

When persistence is introduced, select the simplest approach that satisfies actual requirements.

# Dependency Rules

Every new external dependency must solve a concrete requirement.

Do not add libraries because they may become useful later.

Do not add:

- MediatR
- AutoMapper
- FluentValidation
- generic repository libraries
- event buses
- CQRS frameworks
- cloud SDKs

unless explicitly approved.

Prefer functionality already available in:

- .NET
- Avalonia
- CommunityToolkit
- platform APIs

Before adding a package, determine whether the requirement can reasonably be implemented without it.

# Anti-Slop Rules

Avoid common AI-generated code problems.

Do not introduce:

- unnecessary abstractions
- speculative extensibility
- generic base classes without demonstrated need
- `Manager`, `Helper`, or `Util` classes with unclear ownership
- DTO/model/entity duplication without reason
- broad `catch (Exception)` blocks that hide failures
- excessive comments explaining obvious code
- placeholder architecture
- dead code
- unused interfaces
- unnecessary factories
- service locators
- reflection for simple behavior
- giant methods
- giant classes
- fake tests
- unnecessary design patterns
- unnecessary configuration
- unnecessary indirection

Do not create an interface merely because a class exists.

Do not create a factory merely because an object must be constructed.

Do not create a service merely because logic exists.

Ownership should be obvious from the problem being solved.

# Comments

Comments should explain:

- non-obvious decisions
- platform constraints
- unusual behavior
- important tradeoffs
- reasons behind implementation choices

Do not write comments that merely narrate syntax.

Bad:

```csharp
// Initialize the list
var clips = new List<Clip>();
```

Good:

```csharp
// macOS may not restore the previous application's focus immediately,
// so paste is delayed until activation completes.
```

# Error Handling

Failures must not be silently swallowed.

Avoid:

```csharp
try
{
    ...
}
catch (Exception)
{
}
```

If a failure can be handled meaningfully, handle it.

If it cannot be handled meaningfully at that layer, allow it to propagate to the appropriate boundary.

Platform integration failures should eventually provide enough diagnostic information to understand what failed.

Do not introduce defensive `try/catch` blocks around every operation.

# Testing Rules

Tests should verify meaningful observable behavior.

Avoid tests such as:

```csharp
Assert.NotNull(service);
```

unless successful construction itself is genuinely the behavior under test.

Prefer testing:

- inputs
- outputs
- state transitions
- filtering
- ordering
- edge cases
- failure behavior

Native macOS integration may require manual verification in addition to automated tests.

Do not mock implementation details simply to increase test count.

Do not write tests solely to increase coverage.

A small number of meaningful tests is preferable to a large number of superficial tests.

# Scope Discipline

Before changing code:

1. Read the requested milestone.
2. Read its acceptance criteria.
3. Identify the minimum files that need to change.
4. Implement only the requested behavior.
5. Do not implement adjacent features.
6. Do not refactor unrelated code.
7. Do not redesign existing architecture unless required.

If an architectural problem is discovered outside the requested scope, report it rather than silently redesigning the repository.

If the requested milestone can be completed without introducing a new abstraction, package, or project, prefer the simpler implementation.

# Milestone Discipline

ClipJob is intentionally developed one milestone at a time.

Do not implement functionality belonging to a later milestone.

For example, while implementing search, do not also implement:

- global shortcuts
- persistence
- clipboard management
- native macOS integration

While implementing global shortcuts, do not also implement:

- paste behavior
- foreground application restoration
- SQLite

The current task defines the scope.

The README roadmap does not authorize implementing future functionality.

# Validation

Before claiming a task is complete:

1. Run `dotnet restore`.
2. Run `dotnet build`.
3. Run relevant tests if tests exist.
4. Report build warnings.
5. Review the final diff.
6. Check for accidental scope expansion.
7. Check for unused code.
8. Check for unused dependencies.
9. Check for unrelated formatting changes.

Never claim something was executed successfully if it was not actually executed.

If graphical behavior cannot be validated in the current environment, state that explicitly.

If native macOS behavior requires manual verification, clearly identify what must be tested manually.

# Diff Review

Before completing an implementation task, inspect the complete diff.

Ask:

- Did this task modify only what was necessary?
- Did it introduce unnecessary files?
- Did it introduce unnecessary dependencies?
- Did platform-specific code leak into unrelated layers?
- Did it implement anything belonging to a future milestone?
- Is any code unused?
- Are there unnecessary comments?
- Are names clear?
- Could the implementation be simpler?

Do not automatically defend the implementation.

Look for reasons it may be wrong.

# Git Discipline

Do not commit unless explicitly asked.

Do not push unless explicitly asked.

Do not modify unrelated files.

Do not rewrite Git history.

Do not delete user work unless explicitly requested.

Do not modify README roadmap decisions merely to match an implementation.

Keep each milestone suitable for one focused commit.

# AI-Assisted Development

AI-generated code is not automatically trusted.

Treat generated implementation as a proposed change that must satisfy:

```text
requirements
    ↓
build
    ↓
tests
    ↓
diff review
    ↓
architecture review
    ↓
manual validation where necessary
    ↓
human approval
```

Do not optimize code for appearing sophisticated.

Do not introduce patterns simply because they are common in generated enterprise code.

Readable, boring code is preferred when it solves the problem correctly.

# Current Development Phase

Current phase:

**Phase 0 — macOS Feasibility Spike**

The immediate objective is to prove:

```text
Other application
      ↓
Global shortcut
      ↓
ClipJob search palette
      ↓
Select hardcoded clip
      ↓
Restore previous application
      ↓
Paste selected text
```

The full product must not be built until this interaction is proven reliable.

Refer to `README.md` for the milestone roadmap.

# Current Milestone

**Milestone 1 — Project Foundation**

The current milestone is limited to establishing:

- .NET 10
- Avalonia
- macOS execution
- a basic window
- a search textbox
- three hardcoded clips

Do not implement:

- filtering
- keyboard navigation
- global shortcuts
- clipboard behavior
- native macOS integration
- persistence
- categories
- AI functionality

The current milestone changes only when explicitly instructed.

# Guiding Engineering Principle

> Build the smallest thing that proves the next important assumption.

When choosing between more architecture and less code that correctly satisfies the current requirement, prefer the latter unless the additional architecture solves a concrete problem that exists today.
