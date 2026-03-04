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

## Development Guidelines for Gemini
- **Contextual Awareness:** Always respect the existing architecture, especially the `Terminal.Gui` v2 patterns and the custom UI event bus.
- **Service Registration:** When adding new services or views, ensure they are registered in `Program.cs` or via `ServiceCollectionExtensions`.
- **UI Updates:** UI updates should be triggered by events on the `IUiEventBus` where possible to maintain decoupled components.
- **Testing:** While the project currently lacks a dedicated test suite, aim for testable code by using dependency injection and interfaces.
- **Safety:** Do not modify the project's build or packaging settings unless explicitly requested.
