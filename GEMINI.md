# Muse - Terminal Music Player

## Project Overview
Muse is a cross-platform Terminal User Interface (TUI) music player built with .NET, using **Terminal.Gui** for the interface and **NAudio** for audio playback. It is distributed as a .NET global tool.

## Technical Stack
- **Runtime:** .NET 10.0
- **UI Framework:** Terminal.Gui (v2 develop branch)
- **Audio Engine:** NAudio
- **YouTube Support:** YoutubeExplode
- **Dependency Injection:** Microsoft.Extensions.Hosting

## Architecture & Conventions
- **Dependency Injection:** The application uses `Microsoft.Extensions.Hosting` for service configuration and life-cycle management. All services and views should be registered in the DI container.
- **UI Event Bus:** Communication between UI components and services is handled via an event bus (`IUiEventBus`).
- **Separation of Concerns:**
  - `Muse.App`: Main application entry and high-level orchestration.
  - `Muse.Player`: Audio playback logic and state management.
  - `Muse.UI`: UI components, organized into `Views`. Each view should be a separate class inheriting from a `Terminal.Gui` view (e.g., `Window`, `View`, `Toplevel`).
  - `Muse.YouTube`: YouTube search and download functionality.
  - `Muse.Utils`: Helper classes and global state.
- **Coding Style:**
  - Follow standard .NET naming conventions (PascalCase for classes/methods, camelCase for private fields).
  - Use file-scoped namespaces.
  - Ensure all new services are interfaced (e.g., `IPlayerService` for `PlayerService`).

## Workflow & Git Standards
- **Clean State Check:** Before starting work on a new feature, always verify that there are no uncommitted changes in the current branch. Use `git status` to confirm.
- **Remote Sync:** Before starting any work on a new branch, ensure it has the latest version from the remote (`git pull origin main`).
- **Interruption Policy:** If there are existing uncommitted changes, **stop and notify the user**. Do not proceed with new feature development until the workspace is clean or the user provides explicit instructions on how to handle the changes.
- **Feature Branching:** Always create a new descriptive branch for each feature or bug fix before applying changes.
- **Change Confirmation:** Only commit changes after they have been explicitly confirmed by the user. When asking for confirmation, always include options or a prompt for the user to provide feedback or suggest further improvements.
- **Atomic Commits:** When the work is finished, use an atomic commit approach (small, focused commits that each perform a single logical task) to prepare the Pull Request. Ensure each commit message follows the project's established style.
- **PR Readiness:** Validation (testing and linting) must be completed before proposing or creating a PR.

## Development Guidelines for Gemini
- **Contextual Awareness:** Always respect the existing architecture, especially the `Terminal.Gui` v2 patterns and the custom UI event bus.
- **Service Registration:** When adding new services or views, ensure they are registered in `Program.cs` or via `ServiceCollectionExtensions`.
- **UI Updates:** UI updates should be triggered by events on the `IUiEventBus` where possible to maintain decoupled components.
- **Testing:** While the project currently lacks a dedicated test suite, aim for testable code by using dependency injection and interfaces.
- **Safety:** Do not modify the project's build or packaging settings unless explicitly requested.
