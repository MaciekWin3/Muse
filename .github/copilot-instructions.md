# Copilot instructions for Muse

## Build, test, and pack
- Restore: `dotnet restore Muse/Muse.csproj`
- Build: `dotnet build Muse/Muse.csproj`
- Run locally: `dotnet run --project Muse/Muse.csproj`
- Pack the tool: `dotnet pack Muse/Muse.csproj -c Release --output ./nupkg`
- CI test command: `dotnet test --no-build --verbosity normal`

There is no dedicated test project or single-test selector in the repo yet, so there is no per-test command to use.

## Architecture
- Muse is a .NET 10 terminal music player distributed as a global tool (`muse`).
- `Muse/Program.cs` bootstraps a generic host, requires `MUSE_DIRECTORY`, initializes Terminal.Gui, and registers the core singletons.
- `Muse.App.MuseApp` is the root `Toplevel`; it owns global key handling and app mode switching.
- `Muse.UI.MainWindowView` owns playlist loading, file watching, track transitions, YouTube playlist streaming, and pre-caching.
- UI components in `Muse.UI.Views` communicate through `IUiEventBus`; message types live in `Muse/UI/Bus/UiMessages.cs`.
- Playback is handled by `Muse.Player.PlayerService`, which uses LibVLCSharp for audio and YoutubeExplode for YouTube tracks.
- Shared helpers live in `Muse.Utils`; `Globals` stores app-wide state like `MuseDirectory` and `Volume`.

## Conventions
- Use file-scoped namespaces.
- Keep public services behind interfaces and register them in DI; views are auto-registered with `AddTerminalGuiViews()`.
- Update Terminal.Gui controls on the UI thread with `Application.Invoke(...)`.
- Prefer `Result` / `Result<T>` for expected failures instead of throwing from service methods.
- Add new UI interactions as bus messages rather than coupling views directly.
- Local library scanning uses `MusicListHelper` and includes `mp3`, `mp4`, `m4a`, and `webm` files.
- Respect the existing workflow: check `git status` before editing, avoid touching unrelated local changes, and do not change build/packaging settings unless asked.
