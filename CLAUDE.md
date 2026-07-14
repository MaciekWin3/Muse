# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Muse is a cross-platform terminal music player (TUI) built on .NET 10, distributed as a `dotnet tool` (`MusePlayer` package, `muse` command). It uses **Terminal.Gui v2** (develop branch) for the UI and **LibVLCSharp** (LibVLC) for audio playback, with **YoutubeExplode** for searching/streaming/downloading YouTube audio.

There is a single project, `Muse/Muse.csproj` — no separate class library or test project exists yet.

## Commands

```bash
dotnet build                    # build
dotnet run --project Muse       # run the app (requires MUSE_DIRECTORY, see below)
dotnet pack Muse/Muse.csproj -c Release -o ./nupkg   # produce the nuget/tool package
```

There is no test project in the repo currently (CI's `dotnet test` step is effectively a no-op). If adding tests, follow whatever project layout you introduce — there's no existing convention to match.

### Running the app locally

The app requires the `MUSE_DIRECTORY` environment variable to point at a local music folder before it will start (`Program.cs` exits with an error otherwise):

```powershell
[Environment]::SetEnvironmentVariable("MUSE_DIRECTORY", "C:\Path\To\Your\Music", "User")
```

## Architecture

### Composition root

`Program.cs` builds a generic `Host`, registers services via DI (`Microsoft.Extensions.Hosting`), then hands off to Terminal.Gui's `Application`. `ServiceCollectionExtensions.AddTerminalGuiViews` reflection-scans the assembly and registers every non-abstract `View` subclass as a singleton — new views don't need manual registration, just add the class. Services (`IPlayerService`, `IYoutubeDownloadService`, `IUiEventBus`) are registered explicitly in `Program.cs`.

### Layout (single project, folder-per-concern)

- `Muse/App/MuseApp.cs` — root `Window`; wires global keyboard shortcuts (P=play/pause, N=next, B=previous, D=delete, R=play mode, S=shuffle, Tab=toggle Search/Shortcuts mode) and dispatches them onto the event bus. Key handling is gated by `AppMode` (Shortcuts vs Search) so shortcuts don't fire while a search `TextField` has focus.
- `Muse/Player/` — `PlayerService` (implements `IPlayerService`, defined in the same file) wraps a LibVLC `MediaPlayer`. Handles both local file playback and YouTube streaming (resolves streams via `YoutubeExplode` on demand, with pre-caching support via `PreCacheNextSongs`). `Track` models a playable item (`TrackSource.Local` or `TrackSource.YouTube`).
- `Muse/UI/` — Terminal.Gui views. `MainWindowView` is the main composed view (playlist, controls, progress bar, volume, equalizer) and owns playlist state, YouTube search/download orchestration, and a `FileSystemWatcher` for the music directory. `UI/Views/` holds the individual sub-views (`ControlPanelView`, `MusicListView`, `ProgressBarView`, `VolumeView`, `EqualizerView`, `MenuBarView`, `StatusBarView`).
- `Muse/UI/Bus/` — `IUiEventBus`: an in-process pub/sub bus (`Publish<T>`/`Subscribe<T>`/`Unsubscribe<T>`) that decouples views and services. All cross-component communication (song selection, play/pause, volume, playlist reload, theme changes, mode changes, etc.) goes through typed message records in `UiMessages.cs`, not direct method calls between views. When adding a new interaction, prefer adding a message record and subscribing/publishing over the bus rather than wiring components together directly.
- `Muse/YouTube/YoutubeDownloadService.cs` — downloads single videos or playlists as audio into `MUSE_DIRECTORY` (or a validated relative subfolder — deliberately guards against path traversal outside the base directory), then validates the resulting file is playable via LibVLC before keeping it.
- `Muse/Utils/` — `Globals` (static app-wide state: `MuseDirectory`, `Volume`, UI layout constants), `Result`/`Result<T>` (the error-handling convention used throughout services instead of exceptions — most public service methods return `Result` or `Result<T>` rather than throwing), `MusicListHelper` (scans a directory for `.mp3`/`.mp4`/`.m4a`/`.webm` files), `SpectrumAnalyzer`, `WebsiteHelper`.

### Conventions

- Services are always defined behind an interface (`IPlayerService`, `IYoutubeDownloadService`, `IUiEventBus`) and registered as singletons.
- File-scoped namespaces; standard .NET PascalCase/camelCase naming.
- Fallible operations return `Result` / `Result<T>` (see `Utils/Result.cs`) rather than throwing — check `.Success`/`.IsFailure` at call sites.
- UI-to-service and service-to-UI communication flows through `IUiEventBus`, not direct references between views/services where avoidable.

## Release process

Merges to `main` trigger `.github/workflows/nuget-publish.yml`, which auto-increments the patch version in `Muse/Muse.csproj` (unless it was bumped manually in the PR), tags a GitHub release, and publishes to NuGet. `.github/workflows/build.yml` runs `dotnet build`/`dotnet test` on push/PR to `main`.
