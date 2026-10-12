# Copilot instructions for Muse

## Project commands

Muse is a single .NET 10 executable project and .NET global tool in `Muse/Muse.csproj`.

```bash
# Restore dependencies
dotnet restore

# Build the solution/project
dotnet build
dotnet build Muse/Muse.csproj --no-restore

# Run the application (requires MUSE_DIRECTORY)
dotnet run --project Muse

# Create the global-tool NuGet package
dotnet pack Muse/Muse.csproj --configuration Release --output ./nupkg
```

The application requires `MUSE_DIRECTORY` to point to the music directory. On PowerShell:

```powershell
[Environment]::SetEnvironmentVariable("MUSE_DIRECTORY", "C:\Path\To\Your\Music", "User")
```

CI runs `dotnet restore`, `dotnet build --no-restore`, and `dotnet test --no-build --verbosity normal`. There is currently no test project, so there is no repository-specific single-test command; if a test project is added, run it with `dotnet test <path-to-test-project> --filter "FullyQualifiedName~<test-name>"`.

There is no dedicated lint configuration or lint command in the repository. Builds currently surface compiler, nullable, platform-compatibility, and package warnings.

## Architecture

- `Program.cs` is the composition root. It creates the generic host, registers application services, initializes Terminal.Gui, and starts `MuseApp`.
- `Utils/ServiceCollectionExtensions.cs` reflection-scans the calling assembly and registers every concrete Terminal.Gui `View` as a singleton. New views normally become injectable without an additional registration; non-view services still need explicit registrations in `Program.cs`.
- `App/MuseApp.cs` is the root window and global interaction coordinator. It handles application-level keyboard modes and publishes user actions to the UI event bus.
- `UI/MainWindowView.cs` owns the main playlist and playback orchestration. It composes the child views under `UI/Views`, watches `Globals.MuseDirectory` for filesystem changes, coordinates YouTube search/download actions, and periodically updates playback progress.
- `Player/PlayerService.cs` owns LibVLC playback for local files and YouTube streams. It resolves YouTube audio manifests with YoutubeExplode, caches stream URLs on tracks, and exposes playback state and controls through `IPlayerService`.
- `YouTube/YoutubeDownloadService.cs` handles downloads into the configured music directory and validates downloaded media before retaining it.
- `UI/Bus/` contains `IUiEventBus`, its in-process implementation, and typed message records. Cross-component UI communication should use these messages rather than direct view-to-view calls.
- `Utils/` contains shared application state (`Globals`), directory/media discovery helpers, result types, and other cross-cutting utilities.

## Repository conventions

- Keep services behind interfaces and register them as DI singletons in `Program.cs`; the existing service interfaces are `IPlayerService`, `IYoutubeDownloadService`, and `IUiEventBus`.
- Use file-scoped namespaces, standard .NET naming, and the existing private-field style.
- For fallible service operations, follow the existing `Result`/`Result<T>` convention. Callers inspect `Success` or `IsFailure` and surface failures through the existing UI/error flow instead of relying on exceptions for expected failures.
- Add new UI interactions as typed records in `UI/Bus/UiMessages.cs`, then publish and subscribe through `IUiEventBus`. Keep Terminal.Gui updates on the UI context when invoking from asynchronous work.
- `Globals.MuseDirectory` is the configured base directory for local music. Download and file operations must preserve the existing path validation boundary and must not allow a requested subfolder to escape that base directory.
- `Track` represents both local and YouTube media. Preserve its `TrackSource` distinction and YouTube identifier/stream URL behavior when changing playback or playlist logic.
- Terminal.Gui v2 configuration is applied through `TuiConfigurationBuilder.Shared.ApplyToStaticFacades()`. Theme changes use `ThemeManager.Theme` followed by applying the shared configuration.
- The project is packaged as the `MusePlayer` global tool with command name `muse`. Merges to `main` trigger the NuGet publishing workflow, which auto-increments the patch version unless the project version was manually changed.
- There is no dedicated test suite yet. Keep new logic injectable and isolated so a future test project can exercise services without starting the Terminal.Gui application.
